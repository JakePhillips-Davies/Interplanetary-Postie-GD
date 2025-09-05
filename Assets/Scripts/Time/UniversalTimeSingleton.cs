
using System;
using Godot;

public partial class UniversalTimeSingleton : Node
{
    public static UniversalTimeSingleton inst { get; private set; } = null;

    [Export] public int timeScale { get; private set; } = 1;
    [Export] public bool exponentialTimeScale { get; private set; } = false;
    [Export] public double time { get; private set; } = 0;
    public double timeDelta { get; private set; } = 0;

    public override void _EnterTree() {
        
        if (inst != null)
            if (inst != this)
                GD.Print("ERROR: Multiple instances of " + this + " exists! Are you sure you should be doing this?");

        inst = this;
        
    }

    public override void _PhysicsProcess(double _delta) {
        
        if (exponentialTimeScale) {
            // Get a nice scale while also letting you pause
            int poweredTimeScale = (int)Math.Pow(2, timeScale-1);
            if (timeScale == 0) poweredTimeScale = 0;
            timeDelta = _delta * poweredTimeScale;
        }
        else timeDelta = _delta * timeScale;
        
        time += timeDelta;
        
    }

}
