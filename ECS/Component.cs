namespace Ecs;

public abstract class Component
{
    public readonly GameObject gameObject;

    protected Component(GameObject gameObject)
    {
        this.gameObject = gameObject;
        gameObject.AddComponent(this);
    }
}
