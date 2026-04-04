using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZPG;

public class Shader : IDisposable
{
    public int ID { get; private set; }

    private readonly Dictionary<string, int> uniforms = new();

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
    /// Překlad Shaderu
    /// </summary>
    /// <param name="source"></param>
    /// <param name="type"></param>
    /// <param name="filePath"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
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
    /// Linkování shaderů do výsledného programu.
    /// </summary>
    /// <param name="shaders"></param>
    /// <exception cref="Exception"></exception>
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
    /// Použije program. Nejprve je třeba použít program, pak je možné nastavovat uniforms
    /// </summary>
    public void Use()
    {
        GL.UseProgram(ID);
    }

    /// <summary>
    /// Nahraje adresy všech uniforms ze shaderu
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
    /// Zjistí adresu uniform proměnné ze slovníku, pokud v něm je
    /// </summary>
    /// <param name="name">Jméno uniform</param>
    /// <returns>Adresa</returns>
    public int GetUniformLocation(string name)
    {
        if (uniforms.TryGetValue(name, out int location))
            return location;

        Console.WriteLine($"Warning: Uniform '{name}' not found.");
        return -1;
    }

    /// <summary>
    /// Inteligentní metoda pro nastavování uniform proměnných.
    /// </summary>
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

    bool disposed = false;
    public void Dispose()
    {
        if (disposed) return;

        GL.DeleteProgram(ID);
        disposed = true;
    }
}
