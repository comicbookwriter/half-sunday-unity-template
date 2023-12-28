public abstract class UIView<TModel> : UIInteractable where TModel : struct, IUIModel
{
    public bool IsDestroyed { get; private set; }
    
    public void Destroy()
    {
        gameObject.SetActive(false);
        IsDestroyed = true;
        Destroy(gameObject);
    }

    public abstract void UpdateViewWithModel(TModel model);
}
