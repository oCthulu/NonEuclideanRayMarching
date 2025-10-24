using System.Collections.ObjectModel;
using GpuHlslRayMarchingTest;

namespace Ecs;

public class GameObject
{
    public GameObject? parent;
    public ReadOnlyCollection<Component> Components => components.AsReadOnly();
    protected readonly List<Component> components = [];

    public T? GetComponent<T>() where T : Component
    {
        return components.OfType<T>().FirstOrDefault();
    }

    public void AddComponent(Component component)
    {
        if(component.gameObject != this) throw new InvalidOperationException("Component belongs to a different GameObject");
        if(Components.Contains(component)) throw new InvalidOperationException("Component already added to this GameObject");
        
        components.Add(component);
    }
}