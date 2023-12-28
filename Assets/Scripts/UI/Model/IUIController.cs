using System;

namespace UI.Model
{
    public interface IUIController : IDisposable
    {
        IUIController LogicalParent { get; set; }

        bool IsActiveUI { get; }
        void Show();
        void Hide();
        void Close();
        
        public TController AddChild<TController>(TController childController)
            where TController : IUIController;

        public bool RemoveChild(IUIController childController);
    }
}