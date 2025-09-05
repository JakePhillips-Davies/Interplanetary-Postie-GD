using Godot;

namespace Orbits {

public partial class OrbitSettingsSingleton : Node
{
    public static OrbitSettingsSingleton inst { get; private set; } = null;
    
    [Export] public int patchDepthLimit { get; private set; } = 5;

    [Export] public bool showOrbits { get; private set; } = true;
    [Export] public bool showVelocityDir { get; private set; } = false;
    [Export] public bool showSoi { get; private set; } = false;

    public override void _EnterTree() {
        if (inst != null)
            if (inst != this)
                GD.Print("ERROR: Multiple instances of " + this + " exists! Are you sure you should be doing this?");

        inst = this;
    }
}

}