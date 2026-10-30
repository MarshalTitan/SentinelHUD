using System.Reflection;
using SentinelHUD.Core;

namespace SentinelHUD.Services;

/// <summary>Cached public field/method contract, independently testable without a loaded plugin.</summary>
public sealed class SplatoonElementContract
{
    private readonly object instance;
    private readonly Type elementType;
    private readonly ConstructorInfo constructor;
    private readonly MethodInfo add;
    private readonly MethodInfo remove;
    private readonly Dictionary<string, FieldInfo> fields = new();

    public SplatoonElementContract(object instance)
    {
        this.instance = instance;
        var type = instance.GetType();
        add = type.GetMethod("AddDynamicElements", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new NotSupportedException("Dynamic drawing API unavailable");
        remove = type.GetMethod("RemoveDynamicElements", [typeof(string)])
            ?? throw new NotSupportedException("Dynamic removal API unavailable");
        var args = add.GetParameters();
        if (args.Length != 3 || args[0].ParameterType != typeof(string) || args[2].ParameterType != typeof(long[])
            || !args[1].ParameterType.IsArray || add.ReturnType != typeof(void) || remove.IsStatic)
            throw new NotSupportedException("Dynamic drawing API changed");
        elementType = args[1].ParameterType.GetElementType()!;
        constructor = elementType.GetConstructor([typeof(int)]) ?? throw new NotSupportedException("Element constructor changed");
        var expected = new Dictionary<string, Type>
        {
            ["Name"] = typeof(string), ["Enabled"] = typeof(bool), ["type"] = typeof(int),
            ["refX"] = typeof(float), ["refY"] = typeof(float), ["refZ"] = typeof(float),
            ["offX"] = typeof(float), ["offY"] = typeof(float), ["offZ"] = typeof(float),
            ["radius"] = typeof(float), ["thicc"] = typeof(float), ["color"] = typeof(uint),
            ["overlayText"] = typeof(string), ["overlayTextColor"] = typeof(uint), ["Filled"] = typeof(bool),
            ["refActorType"] = typeof(int), ["refActorComparisonType"] = typeof(int), ["refActorObjectID"] = typeof(uint),
        };
        foreach (var (name, fieldType) in expected)
        {
            var field = elementType.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            if (field is null || field.FieldType != fieldType || field.IsInitOnly)
                throw new NotSupportedException("Element contract changed: " + name);
            fields[name] = field;
        }
    }

    public object Create(EncounterOverlayRequest request)
    {
        var type = request.Shape == EncounterOverlayShape.Line ? 2 : request.FollowActor ? 1 : 0;
        var result = constructor.Invoke([type]); Update(result, request); return result;
    }

    public void Add(string key, object element, long expiresAt)
    {
        var array = Array.CreateInstance(elementType, 1); array.SetValue(element, 0);
        add.Invoke(instance, [key, array, new[] { expiresAt, -2L }]);
    }
    public void Remove(string key) => remove.Invoke(instance, [key]);

    public void Update(object value, EncounterOverlayRequest request)
    {
        void Set(string name, object data) => fields[name].SetValue(value, data);
        var colour = EncounterColourPolicy.Pack(request.Colour);
        Set("Name", request.Id); Set("Enabled", true); Set("color", colour);
        Set("radius", request.Shape is EncounterOverlayShape.Label or EncounterOverlayShape.Line ? 0f : request.Radius);
        Set("thicc", request.Thickness); Set("Filled", request.Filled);
        Set("overlayText", request.Label); Set("overlayTextColor", colour);
        // Public Splatoon coordinates are X,Z,Y rather than the game's X,Y,Z vectors.
        Set("refX", request.Start.X); Set("refY", request.Start.Z); Set("refZ", request.Start.Y);
        Set("offX", request.End.X); Set("offY", request.End.Z); Set("offZ", request.End.Y);
        Set("refActorType", 0); Set("refActorComparisonType", 2); Set("refActorObjectID", request.FollowActor ? request.ActorId : 0u);
    }
}
