using SourceConsole;
using UnityEngine;

public class MemeCube : MonoBehaviour {
    private static MemeCube Singleton;

    private void Awake()
    {
        Singleton = this;
    }

    private void FixedUpdate()
    {
        transform.rotation *= Quaternion.Euler(SpinRate, 0, SpinRate);
    }

    [ConCommand("memeCube_setColor", "Sets the MemeCube's color!")]
    public static void SetColor(float r, float g, float b)
    {
        if (Singleton == null) return;
        Singleton.GetComponent<Renderer>().material.color = new Color(r, g, b);
    }

    [ConCommand("memeCube_otherTest", "Testing the optional parameters")]
    public static int OtherTest(float meme = 2.65f)
    {
        SourceConsole.SourceConsole.print($"Test meme received float: {meme}");
        return 2;
    }

    [ConVar("memeCube_spinRate", "Sets the MemeCube's spinrate!")]
    public static int SpinRate { get; set; }
}
