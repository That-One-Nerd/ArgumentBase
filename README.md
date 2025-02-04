# ArgumentBase

This is a small library that will parse your command-line arguments for you in a pretty easy-to-use fashion.
This library only exists because I wrote this code for a different project and then found myself copy-pasting
it constantly. This is to ease my own suffering.

This is largely purpose-built for me, but feel free to use it. That's why it's here.

## How to use

Import the package using your preferred method. Then, once it's installed, create a class like this:
```csharp
public class YourArguments : ArgumentBase<YourArguments>
{
    // Positional arguments are called "parameters." No prefix is necessary.
    [IsParameter(1, "An example description for the parameter.")]
    public string ExampleParameter { get; set; } // It must be a property with a set method or it won't be identified.

    // Variables are formatted like this: "-var:3.14"
    [IsVariable("var", "An example description for the variable.")]
    public double ExampleVariable { get; set; } // Parameters and variables can be any object that derives from IParsable.

    // Flags are formatted like this: "--flag"
    [IsFlag("flag", "An example description for the flag.")]
    public bool ExampleFlag { get; set; } // Flags, however, can only be booleans.
    // Whenever a flag is included as an argument, its value is flipped.
}
```

Then, in your Main method, paste the following:
```csharp
static void Main(string[] args)
{
    YourArguments args = YourArguments.Parse(args);
}
```

That's it. Not too bad, right?
