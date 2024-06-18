// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MonoGame.Effect.TPGParser;

namespace MonoGame.Effect
{
    class MetalShaderProfile : ShaderProfile
    {
        private static readonly Regex HlslPixelShaderRegex =
            new Regex(@"^ps_(?<major>1|2|3|4|5)_(?<minor>0|1|)(_level_(9_1|9_2|9_3))?$", RegexOptions.Compiled);

        private static readonly Regex HlslVertexShaderRegex =
            new Regex(@"^vs_(?<major>1|2|3|4|5)_(?<minor>0|1|)(_level_(9_1|9_2|9_3))?$", RegexOptions.Compiled);

        public MetalShaderProfile()
            : base("Metal", 2)
        {
        }

        internal override void AddMacros(Dictionary<string, string> macros)
        {
            macros.Add("HLSL", "1");
            macros.Add("SM4", "1");
        }

        internal override void ValidateShaderModels(PassInfo pass)
        {
        }

        internal override ShaderData CreateShader(ShaderResult shaderResult, string shaderFunction,
            string shaderProfile, bool isVertexShader, EffectObject effect, ref string errorsAndWarnings)
        {
            // Using the GLSL version for now to capture all the parameters, constants/vertex attributes etc.
            // The Metal bytecode will be the only change at the end.
            var bytecode = EffectObject.CompileHLSL(shaderResult, shaderFunction, shaderProfile, ref errorsAndWarnings);

            var shaderInfo = shaderResult.ShaderInfo;
            var shaderData = ShaderData.CreateHLSL(bytecode, isVertexShader, effect.ConstantBuffers,
                effect.Shaders.Count, shaderInfo.SamplerStates, shaderResult.Debug);
            var macros = new Dictionary<string, string>();
            AddMacros(macros);
            ShaderData.CreateSpirVFromFX(shaderData, shaderResult.Options, isVertexShader,
                effect.ConstantBuffers, effect.Shaders.Count, shaderInfo.SamplerStates, shaderResult.Debug,
                shaderFunction, shaderInfo, macros, shaderResult);
            shaderData = ShaderData.CreateMetalFromSpirV(shaderData, shaderResult.Options, isVertexShader,
                effect.ConstantBuffers, effect.Shaders.Count, shaderInfo.SamplerStates, shaderResult.Debug,
                shaderFunction, shaderInfo, macros, shaderResult);
            effect.Shaders.Add(shaderData);
            return shaderData;
        }
    }
}
