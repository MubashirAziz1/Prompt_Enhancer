using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PromptEnhancer
{
    public sealed class DpapiCredentialStore : ICredentialStore
    {
        private readonly string path;

        public DpapiCredentialStore(string path) { this.path = Path.GetFullPath(path); }

        public string Load()
        {
            byte[] clear = null;
            try
            {
                byte[] encrypted;
                try { encrypted = File.ReadAllBytes(path); }
                catch (FileNotFoundException) { return null; }
                catch (DirectoryNotFoundException) { return null; }
                clear = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(clear);
            }
            catch { throw new CredentialException(); }
            finally { if (clear != null) Array.Clear(clear, 0, clear.Length); }
        }

        public void Save(string credential)
        {
            if (String.IsNullOrWhiteSpace(credential)) throw new CredentialException();
            byte[] clear = Encoding.UTF8.GetBytes(credential.Trim());
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                byte[] encrypted = ProtectedData.Protect(clear, null, DataProtectionScope.CurrentUser);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    file.Write(encrypted, 0, encrypted.Length);
                    file.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
            }
            catch { throw new CredentialException(); }
            finally
            {
                Array.Clear(clear, 0, clear.Length);
                try { if (File.Exists(temporary)) File.Delete(temporary); } catch { }
            }
        }
    }
}
