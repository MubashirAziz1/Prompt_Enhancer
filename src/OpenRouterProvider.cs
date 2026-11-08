using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PromptEnhancer
{
    public sealed class OpenRouterProvider : IEnhancementProvider
    {
        public const string InitialModel = "openai/gpt-4.1-mini";
        private static readonly Uri Endpoint = new Uri("https://openrouter.ai/api/v1/chat/completions");
        private readonly HttpClient client;
        private readonly ICredentialStore credentials;
        private readonly string model;

        public OpenRouterProvider(HttpClient client, ICredentialStore credentials, string model)
        {
            this.client = client;
            this.credentials = credentials;
            this.model = model;
        }

        public async Task<string> EnhanceAsync(string roughPrompt, string policy, CancellationToken cancellation)
        {
            string key = credentials.Load();
            if (String.IsNullOrWhiteSpace(key)) throw new EnhancementException(EnhancementFailure.MissingCredential);
            try
            {
                var payload = new ChatRequest {
                    Model = model, Temperature = 0.2, MaxTokens = 2048, Stream = false,
                    Messages = new[] { new ChatMessage { Role = "system", Content = policy },
                        new ChatMessage { Role = "user", Content = roughPrompt } }
                };
                byte[] json;
                using (var stream = new MemoryStream())
                {
                    new DataContractJsonSerializer(typeof(ChatRequest)).WriteObject(stream, payload);
                    json = stream.ToArray();
                }
                using (var request = new HttpRequestMessage(HttpMethod.Post, Endpoint))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    request.Content = new ByteArrayContent(json);
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    using (var response = await client.SendAsync(request, cancellation).ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode) throw FailureFor(response.StatusCode);
                        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        ChatResponse completion;
                        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
                            completion = (ChatResponse)new DataContractJsonSerializer(typeof(ChatResponse)).ReadObject(stream);
                        if (completion == null || completion.Error != null || completion.Choices == null ||
                            completion.Choices.Length == 0 || completion.Choices[0] == null ||
                            completion.Choices[0].FinishReason != "stop" || completion.Choices[0].Message == null ||
                            completion.Choices[0].Message.Role != "assistant" ||
                            String.IsNullOrWhiteSpace(completion.Choices[0].Message.Content))
                            throw new EnhancementException(EnhancementFailure.InvalidResponse);
                        string result = completion.Choices[0].Message.Content.Trim();
                        // Even a misbehaving service must not echo the authentication secret into the UI.
                        if (result.IndexOf(key, StringComparison.Ordinal) >= 0)
                            throw new EnhancementException(EnhancementFailure.InvalidResponse);
                        return result;
                    }
                }
            }
            catch (EnhancementException) { throw; }
            catch (OperationCanceledException)
            {
                if (cancellation.IsCancellationRequested) throw new OperationCanceledException(cancellation);
                throw new EnhancementException(EnhancementFailure.Unavailable);
            }
            catch (SerializationException) { throw new EnhancementException(EnhancementFailure.InvalidResponse); }
            catch { throw new EnhancementException(EnhancementFailure.Unavailable); }
        }

        private static EnhancementException FailureFor(HttpStatusCode status)
        {
            switch ((int)status)
            {
                case 401: case 403: return new EnhancementException(EnhancementFailure.Authentication);
                case 402: return new EnhancementException(EnhancementFailure.Credits);
                case 429: return new EnhancementException(EnhancementFailure.RateLimit);
                default: return new EnhancementException(EnhancementFailure.Unavailable);
            }
        }

        [DataContract]
        private sealed class ChatRequest
        {
            [DataMember(Name = "model")] public string Model;
            [DataMember(Name = "messages")] public ChatMessage[] Messages;
            [DataMember(Name = "temperature")] public double Temperature;
            [DataMember(Name = "max_tokens")] public int MaxTokens;
            [DataMember(Name = "stream")] public bool Stream;
        }

        [DataContract]
        private sealed class ChatMessage
        {
            [DataMember(Name = "role")] public string Role;
            [DataMember(Name = "content")] public string Content;
        }

        [DataContract]
        private sealed class ChatResponse
        {
            [DataMember(Name = "choices")] public Choice[] Choices { get; set; }
            [DataMember(Name = "error")] public object Error { get; set; }
        }

        [DataContract]
        private sealed class Choice
        {
            [DataMember(Name = "message")] public ChatMessage Message { get; set; }
            [DataMember(Name = "finish_reason")] public string FinishReason { get; set; }
        }
    }
}
