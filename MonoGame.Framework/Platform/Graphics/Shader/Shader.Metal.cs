// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using Metal;
using static System.Text.Encoding;
using GD = Microsoft.Xna.Framework.Graphics.GraphicsDebug;

namespace Microsoft.Xna.Framework.Graphics
{
    /// <summary>
    /// Connects a MonoGame Shader to a Metal Shader.
    /// 
    /// Checkout an end-to-end example first to understand the pipeline first:
    /// <see cref="https://developer.apple.com/documentation/metal/using_a_render_pipeline_to_render_primitives#3994505"/>
    ///
    /// Shaders are specified as part of the render pipeline descriptor to generate a pipeline state.
    /// Pipeline states are expensive to create but mandatory in a render encoder.
    ///
    /// Note: Switching between pipeline states is not as expensive in Metal as it was in OpenGL
    /// given that the render encoder implicitly orders the instructions in the command buffer
    /// without as opposed to saving/restoring states as we did in OpenGL.
    /// 
    /// </summary>
    internal partial class Shader
    {
        private IMTLLibrary _shaderLibrary;

        public IMTLFunction Program { get; private set; }

        /// <summary>
        /// Must match 
        /// <seealso href="https://github.com/search?q=repo%3AMonoGame%2FMonoGame+shaderprofile&type=code" />
        /// Must match the profile for <code>Tools\MonoGame.Effect.Compiler\Effect\ShaderProfile.Metal.cs</code>
        /// </summary>
        private static int PlatformProfile()
        {
            return 2;
        }

        private void PlatformConstruct(ShaderStage stage, byte[] shaderBytecode)
        {
            HashKey = MonoGame.Framework.Utilities.Hash.ComputeHash(shaderBytecode);
            try
            {
                GD.C(
                    $"Loading metal lib {Name} cbuffers: {CBuffers.Length} / Samplers {Samplers.Length} / Attributes {Attributes.Length}");

                // We assume the sources are directly present; this allows us to have one
                // Shader source that works on all Apple platforms including the simulator.
                // This is similar in spirit to OpenGL GLSL sources embedded in mgfxo.
                var shaderSource = UTF8.GetString(shaderBytecode);
                // MSL Version 1.2.0 allows us to target the lowest Metal-supported devices such
                // as iPad Mini 2, iPhone 5s. See https://developer.apple.com/support/required-device-capabilities/#iphone-devices
                var compileOptions = new MTLCompileOptions() { LanguageVersion = MTLLanguageVersion.v1_2 };

                // In theory we could use the async compilation but based on profiling and the way
                // MonoGame is setup, the effect needs to be functional by the time the `PlatformConstruct`
                // has returned. Also, even on the lowest-end iOS devices, the compilation is sub-1ms
                // where it will likely be overshadowed by other resource loading (other IO bound stuff
                // happening) that async compilation won't have a meaningful perf improvement and likely
                // increase complexity. This also matches the OpenGL behavior as everything is single-threaded.
                _shaderLibrary = GraphicsDevice.MetalDevice.CreateLibrary(
                    shaderSource, compileOptions, out var error);
                if (_shaderLibrary == null)
                {
                    GD.C($"Unable to load metallib {Name} {error}");
                    _ResetShader();
                    return;
                }

                //GD.C($"Loaded metal lib {_shaderLibrary.Label}: {Stage}");
                foreach (var func in _shaderLibrary.FunctionNames) { GD.C($" -- Found {func}"); }

                Program = _shaderLibrary.CreateFunction("main0");
                _FillVertexAttributeLocations();
                PrintInfo();
            }
            catch (Exception e)
            {
                GD.C($"Error loading metallib {Name} : {e}");
                _ResetShader();
            }
        }

        private void _ResetShader()
        {
            MetalGraphicsHelpers.CleanDispose(ref _shaderLibrary);
            Program?.Dispose();
            Program = null;
        }

        private void _FillVertexAttributeLocations()
        {
            if (Program.VertexAttributes?.Length > 0)
            {
                GD.C($"Vertex Attributes length: {Attributes.Length} / {Program.VertexAttributes.Length}");
                if (Attributes?.Length != Program.VertexAttributes.Length)
                {
                    Attributes = new VertexAttribute[(int)Program.VertexAttributes.Length];
                }

                for (var index = 0; index < Program.VertexAttributes.Length; index++)
                {
                    var attr = Program.VertexAttributes[index];
                    Attributes[index].location = (int)attr.AttributeIndex;
                    Attributes[index].index = index;
                }
            }
        }

        internal int GetAttribLocation(VertexElementUsage usage, int index)
        {
            for (int i = 0; i < Attributes.Length; ++i)
            {
                if ((Attributes[i].index == index)) // TODO: check usage
                    return Attributes[i].location;
            }

            return -1;
        }

        internal void ApplySamplerTextureUnits(int program)
        {
        }

        private void PlatformGraphicsDeviceResetting()
        {
        }

        protected override void Dispose(bool disposing)
        {
            _ResetShader();
            base.Dispose(disposing);
        }

        public void PrintInfo()
        {
            if (Program == null) { return; }

            GD.C(
                $"------- Shader {Program.Name} / {_shaderLibrary.Label} ");

            if (Program.VertexAttributes is { } attrs)
            {
                for (int i = 0; i < attrs.Length; ++i)
                {
                    var attr = attrs[i];
                    GD.C($"-- ${i}: {attr.Name} : {attr.AttributeType} / {attr.AttributeIndex}");
                }
            }

            foreach (var cb in Program.FunctionConstants) { GD.C($" ----- shader constant: {cb.Key}: {cb.Value}"); }
        }
    }
}
