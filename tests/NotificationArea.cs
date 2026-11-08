using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Automation;

namespace PromptEnhancer.Tests
{
    // Find the actual shell icon through accessibility, including the tray overflow.
    internal static class NotificationArea
    {
        public static void OpenPromptEnhancerMenu()
        {
            Point? icon = FindButton("Prompt Enhancer");
            if (!icon.HasValue)
            {
                Point? overflow = FindButton("Show hidden icons");
                TestRunner.Require(overflow.HasValue, "The notification icon or tray overflow must be accessible.");
                DesktopMouse.ClickAt(overflow.Value, false);
                TestRunner.PumpUntil(() => (icon = FindButton("Prompt Enhancer")).HasValue);
            }
            DesktopMouse.ClickAt(icon.Value, true);
        }

        private static Point? FindButton(string name)
        {
            // UI Automation runs off the application's UI thread to avoid reentrancy/deadlocks.
            var query = Task.Run(() => {
                var windows = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);
                foreach (AutomationElement window in windows)
                {
                    string className = window.Current.ClassName;
                    if (className != "Shell_TrayWnd" && className != "NotifyIconOverflowWindow" &&
                        className != "TopLevelWindowForOverflowXamlIsland") continue;
                    var buttons = window.FindAll(TreeScope.Descendants, new AndCondition(
                        new PropertyCondition(AutomationElement.NameProperty, name, PropertyConditionFlags.IgnoreCase),
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button)));
                    foreach (AutomationElement button in buttons)
                    {
                        var bounds = button.Current.BoundingRectangle;
                        if (!button.Current.IsOffscreen && !bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
                            return (Point?)new Point((int)(bounds.Left + bounds.Width / 2), (int)(bounds.Top + bounds.Height / 2));
                    }
                }
                return (Point?)null;
            });
            TestRunner.PumpUntil(() => query.IsCompleted);
            return query.GetAwaiter().GetResult();
        }
    }
}
