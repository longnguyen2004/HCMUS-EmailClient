using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EmailClient.Gui.Control
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:EmailClient.Gui.Control"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:EmailClient.Gui.Control;assembly=EmailClient.Gui.Control"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:ClosableTabItem/>
    ///
    /// </summary>
    public class CloseableTabItem : TabItem
    {
        static CloseableTabItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CloseableTabItem), new FrameworkPropertyMetadata(typeof(CloseableTabItem)));
        }

        public static readonly RoutedEvent CloseTabEvent =
            EventManager.RegisterRoutedEvent(
                nameof(CloseTab), RoutingStrategy.Bubble,
                typeof(RoutedEventHandler), typeof(CloseableTabItem)
            );
        
        public event RoutedEventHandler CloseTab
        {
            add { AddHandler(CloseTabEvent, value); }
            remove { RemoveHandler(CloseTabEvent, value); }
        }

        public event CancelEventHandler? Closing;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild("CloseButton") is Button closeButton)
                closeButton.Click += closeButton_Click;
        }

        void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        public void Close()
        {
            CancelEventArgs args = new(false);
            Closing?.Invoke(this, args);
            if (args.Cancel) return;
            (Parent as TabControl)?.Items.Remove(this);
        }

    }
}
