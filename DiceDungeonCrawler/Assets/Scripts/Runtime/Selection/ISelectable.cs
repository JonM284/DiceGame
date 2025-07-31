namespace Runtime.Selection
{
    public interface ISelectable
    {
        public abstract void OnSelect();
        public abstract void OnUnselect();
        public abstract void OnHoverStart();
        public abstract void OnHoverEnd();
    }
}