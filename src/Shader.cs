using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZPG;

/// <summary>
/// OpenGL shader program wrapper handling compilation, linking and uniforms.
/// </summary>
public class Shader : IDisposable
{
    private readonly Dictionary<string, int> uniforms = new();
    private bool disposed = false;

    /// <summary>
    /// OpenGL program object identifier.
    /// </summary>
    public int ID { get; private set; }

    /// <summary>
    /// Compiles and links vertex/fragment shader files into a program.
    /// </summary>
    /// <param name="vertexPath">Path to vertex shader source.</param>
    /// <param name="fragmentPath">Path to fragment shader source.</param>
    public Shader(string vertexPath, string fragmentPath)
    {
        int vertexShader = CompileShader(vertexPath, ShaderType.VertexShader);
        int fragmentShader = CompileShader(fragmentPath, ShaderType.FragmentShader);
        LinkShader(vertexShader, fragmentShader);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        LoadUniforms();
    }

    /// <summary>
    /// Compiles a shader source file for the specified shader stage.
    /// </summary>
    /// <param name="filePath">Path to shader source file.</param>
    /// <param name="type">OpenGL shader stage.</param>
    /// <returns>Compiled shader object id.</returns>
    /// <exception cref="Exception">Thrown when shader compilation fails.</exception>
    private int CompileShader(string filePath, ShaderType type)
    {
        string source = File.ReadAllText(filePath);
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string log = GL.GetShaderInfoLog(shader);
            Console.WriteLine($"Error compiling {type} shader ({filePath}):\n{log}\n");
            throw new Exception($"Shader compilation failed for {filePath}");
        }

        return shader;
    }

    /// <summary>
    /// Links compiled shaders into a final OpenGL program.
    /// </summary>
    /// <param name="shaders">Compiled shader object ids.</param>
    /// <exception cref="Exception">Thrown when program linking fails.</exception>
    private void LinkShader(params int[] shaders)
    {
        ID = GL.CreateProgram();
        foreach (int shader in shaders)
        {
            GL.AttachShader(ID, shader);
        }
        GL.LinkProgram(ID);

        GL.GetProgram(ID, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string log = GL.GetProgramInfoLog(ID);
            throw new Exception($"Error linking shader program:\n{log}");
        }
    }

    /// <summary>
    /// Binds this shader program as the current active program.
    /// </summary>
    public void Use()
    {
        GL.UseProgram(ID);
    }

    /// <summary>
    /// Caches active uniform locations from the linked program.
    /// </summary>
    private void LoadUniforms()
    {
        GL.GetProgram(ID, GetProgramParameterName.ActiveUniforms, out int uniformCount);

        for (int i = 0; i < uniformCount; i++)
        {            
            GL.GetActiveUniform(ID, i, 256, out _, out _, out _, out string name);

            string uniformName = name.ToString();
            int location = GL.GetUniformLocation(ID, uniformName);

            if (location != -1)
            {
                uniforms[uniformName] = location;
                Console.WriteLine($"Loaded uniform: {uniformName} -> {location}");
            }
        }
    }

    /// <summary>
    /// Gets cached location for a uniform name.
    /// </summary>
    /// <param name="name">Uniform variable name.</param>
    /// <returns>Uniform location or -1 if not found.</returns>
    public int GetUniformLocation(string name)
    {
        if (uniforms.TryGetValue(name, out int location))
            return location;

        Console.WriteLine($"Warning: Uniform '{name}' not found.");
        return -1;
    }

    /// <summary>
    /// Sets a uniform value by type.
    /// </summary>
    /// <typeparam name="T">Uniform value type.</typeparam>
    /// <param name="name">Uniform variable name.</param>
    /// <param name="value">Uniform value.</param>
    public void SetUniform<T>(string name, T value)
    {
        int location = GetUniformLocation(name);
        if (location == -1) return;

        switch (value)
        {
            case int v:
                GL.Uniform1(location, v);
                break;
            case float v:
                GL.Uniform1(location, v);
                break;
            case Vector2 v:
                GL.Uniform2(location, v);
                break;
            case OpenTK.Mathematics.Vector3 v:
                GL.Uniform3(location, v);
                break;
            case Vector4 v:
                GL.Uniform4(location, v);
                break;
            case Matrix4 v:
                GL.UniformMatrix4(location, false, ref v);
                break;
            default:
                throw new NotSupportedException($"Uniform type {typeof(T)} is not supported.");
        }
    }

    /// <summary>
    /// Deletes the OpenGL shader program.
    /// </summary>
    public void Dispose()
    {
        if (disposed) return;

        GL.DeleteProgram(ID);
        disposed = true;
    }
}
