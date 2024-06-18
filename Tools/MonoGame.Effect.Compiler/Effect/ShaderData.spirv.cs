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
        /// <summary>
        /// Converts HLSL to SPIR-V first to then be consumed by other code.
        /// </summary>
        public static ShaderData CreateSpirVFromFX(ShaderData shaderData, Options options, bool isVertexShader,
            List<ConstantBufferData> cbuffers, int sharedIndex, Dictionary<string, SamplerStateInfo> samplerStates,
            bool debug, string shaderFunctionName, ShaderInfo shaderInfo, Dictionary<string, string> macros,
            ShaderResult shaderResult)
        {
            Console.WriteLine(
                $"Converting HLSL->SPIRV for {shaderFunctionName} in {shaderResult.FilePath} / constant buffers #{cbuffers.Count} / samplers #{samplerStates.Count}");

            string dxcTool = "dxc";
            {
                // Convert from HLSL to SpirV - output is in a temporary directory.
                var tempFile = Path.GetTempFileName();
                var spirvFile = tempFile + ".spv";
                var targetProfile =
                    isVertexShader ? "vs_6_0" : "ps_6_0"; // MGFX SM4 is too old! But latest versions work well.
                // See this doc for FXC->DXC porting guide: https://github.com/microsoft/DirectXShaderCompiler/wiki/Porting-shaders-from-FXC-to-DXC
                var additionalOptions = " -fvk-use-gl-layout " + // The Vulkan flags are probably unnecessary.
                                        " -fvk-auto-shift-bindings " +
                                        " -flegacy-macro-expansion " + // Maintain legacy behavior (DX9-ish)
                                        " -flegacy-resource-reservation " + // Maintain legacy behavior (DX9-ish)
                                        " -no-warnings " + // Warnings spook the MGCB pipeline and disrupts the flow.
                                        "  ";

                foreach (var kv in macros) { additionalOptions += $" -D {kv.Key} {kv.Value} "; }

                var retCode = ExternalTool.Run(dxcTool,
                    $" -spirv -T {targetProfile} -E {shaderFunctionName} {additionalOptions}  -Fo {spirvFile} {shaderResult.FilePath}", // Make sure the shader file is at the end.
                    out var stdout, out var stderr);
                if (retCode != _SUCCESS_RETURN_CODE || !File.Exists(spirvFile))
                {
                    throw new Exception($"Unable to convert hlsl file to spirv:\n{stdout}\n{stderr}");
                }

                // TODO Also set:
                /*
                  -Fo <file>              Output object file
                  -Fre <file>             Output reflection to the given file
                  -Frs <file>             Output root signature to the given file
                  -Fsh <file>             Output shader hash to the given file
                                 */
                if (!string.IsNullOrEmpty(stdout) && !string.IsNullOrEmpty(stderr))
                {
                    Console.WriteLine($"{stdout}\n{stderr}");
                }

                Console.WriteLine($" ---- SpirV written to {spirvFile}");
                shaderData.SpirVOutputFile = spirvFile;
            }

            return shaderData;
        }

        public string SpirVOutputFile { get; set; }
    }
}
