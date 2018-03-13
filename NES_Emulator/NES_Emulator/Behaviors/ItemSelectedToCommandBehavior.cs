using System.Windows.Input;
using Prism.Behaviors;
using Xamarin.Forms;

namespace NES_Emulator.Behaviors
{
    public class ItemSelectedToCommandBehavior : BehaviorBase<ListView>
    {
        public static readonly BindableProperty CommandProperty = BindableProperty.Create("Command", typeof(ICommand), typeof(EventToCommandBehavior));
        public ICommand Command
        {
            get { return (ICommand)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

		protected override void OnAttachedTo(ListView bindable)
		{
            base.OnAttachedTo(bindable);
            bindable.ItemSelected += (s, e) =>
            {
                if (e.SelectedItem != null)
                {
                    if (Command != null) Bindable_ItemSelected(s, e);
                    AssociatedObject.SelectedItem = null;
                }
            };
		}

		protected override void OnDetachingFrom(ListView bindable)
		{
            base.OnDetachingFrom(bindable);
            bindable.ItemSelected -= Bindable_ItemSelected;
		}

        protected virtual void Bindable_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (Command.CanExecute(e.SelectedItem)) Command.Execute(e.SelectedItem);   
        }
	}
}
