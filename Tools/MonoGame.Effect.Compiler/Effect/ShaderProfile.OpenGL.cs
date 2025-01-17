﻿// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MonoGame.Effect.TPGParser;

namespace MonoGame.Effect
{
    class OpenGLShaderProfile : ShaderProfile
    {
        private static readonly Regex GlslPixelShaderRegex = new Regex(@"^ps_(?<major>1|2|3|4|5)_(?<minor>0|1|)$", RegexOptions.Compiled);
        private static readonly Regex GlslVertexShaderRegex = new Regex(@"^vs_(?<major>1|2|3|4|5)_(?<minor>0|1|)$", RegexOptions.Compiled);

        public OpenGLShaderProfile()
            : base("OpenGL", 0)
        {                
        }

        internal override void AddMacros(Dictionary<string, string> macros)
        {
            macros.Add("GLSL", "1");
            macros.Add("OPENGL", "1");                
        }

        internal override void ValidateShaderModels(PassInfo pass)
        {
            int major, minor;

            if (!string.IsNullOrEmpty(pass.vsFunction))
            {
                ParseShaderModel(pass.vsModel, GlslVertexShaderRegex, out major, out minor);
                if (major > 3)
                    throw new Exception(String.Format("Invalid profile '{0}'. Vertex shader '{1}' must be SM 3.0 or lower!", pass.vsModel, pass.vsFunction));
            }

            if (!string.IsNullOrEmpty(pass.psFunction))
            {
                ParseShaderModel(pass.psModel, GlslPixelShaderRegex, out major, out minor);
                if (major > 3)
                    throw new Exception(String.Format("Invalid profile '{0}'. Pixel shader '{1}' must be SM 3.0 or lower!", pass.vsModel, pass.psFunction));
            }
        }

        internal override ShaderData CreateShader(ShaderResult shaderResult, string shaderFunction, string shaderProfile, bool isVertexShader, EffectObject effect, ref string errorsAndWarnings)
        {
            // For now GLSL is only supported via translation
            // using MojoShader which works from HLSL bytecode.
            var bytecode = EffectObject.CompileHLSL(shaderResult, shaderFunction, shaderProfile, ref errorsAndWarnings);

            var shaderInfo = shaderResult.ShaderInfo;
            var shaderData = ShaderData.CreateGLSL(bytecode, isVertexShader, effect.ConstantBuffers, effect.Shaders.Count, shaderInfo.SamplerStates, shaderResult.Debug);
            if (shaderResult.Options.OutputRaw)
            {
                var copyPathTo = $"{shaderResult.FilePath}.{(isVertexShader ? "vert" : "frag")}.glsl";
                copyPathTo = ShaderData.ConvertRawOutputPath(copyPathTo);
                Console.WriteLine($"-- Writing raw GLSL file to {copyPathTo}");
                File.WriteAllBytes(path: copyPathTo, shaderData.ShaderCode);
                
            }
            effect.Shaders.Add(shaderData);

            return shaderData;
        }
    }
}
