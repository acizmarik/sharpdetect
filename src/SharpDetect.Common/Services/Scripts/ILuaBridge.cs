using MoonSharp.Interpreter;
using SharpDetect.Common.Scripts;

namespace SharpDetect.Common.Services.Scripts
{
    public interface ILuaBridge
    {
        string[] ModuleDirectories { get; }

        Task<Script> LoadModuleAsync(string path);

        AssemblyDescriptorScript CreateAssemblyDescriptor(Script script);
    }
}
