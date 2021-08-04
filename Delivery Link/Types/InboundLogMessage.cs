using System;
using System.Windows;

namespace Delivery_Link.Types
{
    public static class InboundLogMessage
    {

        public static readonly DependencyProperty MyPropertyProperty = DependencyProperty.RegisterAttached("callsign",
            typeof(string), typeof(InboundLogMessage), new FrameworkPropertyMetadata(null));

        public static string GetMyProperty(UIElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            return (string)element.GetValue(MyPropertyProperty);
        }
        public static void SetMyProperty(UIElement element, string value)
        {
            if (element == null)
                throw new ArgumentNullException("element");
            element.SetValue(MyPropertyProperty, value);
        }
    }
}
