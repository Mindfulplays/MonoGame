// MonoGame - Copyright (C) MonoGame Foundation, Inc
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;
using MonoGame.Effect.TPGParser;

namespace MonoGame.Effect
{
    internal partial class ShaderData
    {
        private const int _SUCCESS_RETURN_CODE = 0;

        // For debugging purposes.
        internal byte[] MetalShaderBytes { get; set; }

        /// <summary>
        /// Converts SPIR-V to Metal first to then be consumed by other code.
        /// TODO: Cleanup unnecessary params.
        /// </summary>
        public static ShaderData CreateMetalFromSpirV(ShaderData shaderData, Options options, bool isVertexShader,
            List<ConstantBufferData> cbuffers, int sharedIndex, Dictionary<string, SamplerStateInfo> samplerStates,
            bool debug, string shaderFunctionName, ShaderInfo shaderInfo, Dictionary<string, string> macros,
            ShaderResult shaderResult)
        {
            Console.WriteLine($"Converting SPIRV->MSL for {shaderFunctionName} in {shaderResult.FilePath}");
            string spirVCrossTool = "spirv-cross";

            {
                // Convert from HLSL to SpirV - output is in a temporary directory.
                var spirvFile = shaderData.SpirVOutputFile;
                if (string.IsNullOrEmpty(spirvFile) || !File.Exists(spirvFile))
                {
                    throw new Exception($"SpirV output is missing {spirvFile}");
                }

                shaderData.MetalOutputFile = Path.ChangeExtension(shaderData.SpirVOutputFile, ".msl");
                var additionalOptions =
                    $"--rename-entry-point {shaderFunctionName} main {(isVertexShader ? "vert" : "frag")} " +
                    $"";
                // MSL Version: MMmmpp (1.2.0) - this allows us to target the lowest Metal-supported devices such
                // as iPad Mini 2, iPhone 5s. See https://developer.apple.com/support/required-device-capabilities/#iphone-devices
                // Note that this generates code that is supported by the lowest version: we still need to nudge
                // the compiler (at runtime for instance) to explicitly provide the compiler version via MTLCompileOptions MTLLanguageVersion.
                if (ExternalTool.Run(spirVCrossTool,
                        $"--msl --msl-ios  --msl-version 10200 {additionalOptions} --output {shaderData.MetalOutputFile} {shaderData.SpirVOutputFile}",
                        out var stdout, out var stderr) != _SUCCESS_RETURN_CODE ||
                    !File.Exists(shaderData.MetalOutputFile))
                {
                    throw new Exception($"Unable to convert spirv to metal:\n{stdout}\n{stderr}");
                }

                Console.WriteLine($" -- MetalSL written to {shaderData.MetalOutputFile}");
                var metalBytes = File.ReadAllBytes(shaderData.MetalOutputFile);
                shaderData.MetalShaderBytes = metalBytes;
                shaderData.ShaderCode = metalBytes;
            }

            return shaderData;
        }

        public string MetalOutputFile { get; set; }
    }
}
