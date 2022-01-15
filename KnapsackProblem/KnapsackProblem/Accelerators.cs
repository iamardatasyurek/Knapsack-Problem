﻿using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System;
using System.IO;

namespace KnapsackProblem
{
    class Accelerators
    {
        public Accelerators()
        {
            // Builds a context that has all possible accelerators.
            using Context context = Context.CreateDefault();

            // Builds a context that only has CPU accelerators.
            //using Context context = Context.Create(builder => builder.CPU());

            // Builds a context that only has Cuda accelerators.
            //using Context context = Context.Create(builder => builder.Cuda());

            // Builds a context that only has OpenCL accelerators.
            //using Context context = Context.Create(builder => builder.OpenCL());

            // Builds a context with only OpenCL and Cuda acclerators.
            //using Context context = Context.Create(builder =>
            //{
            //    builder
            //        .OpenCL()
            //        .Cuda();
            //});

            // Prints all accelerators.
            foreach (Device d in context)
            {
                using Accelerator accelerator = d.CreateAccelerator(context);
                Console.WriteLine(accelerator);
                Console.WriteLine(GetInfoString(accelerator));
            }

            // Prints all CPU accelerators.
            foreach (CPUDevice d in context.GetCPUDevices())
            {
                using CPUAccelerator accelerator = (CPUAccelerator)d.CreateAccelerator(context);
                Console.WriteLine(accelerator);
                Console.WriteLine(GetInfoString(accelerator));
            }

            // Prints all Cuda accelerators.
            foreach (Device d in context.GetCudaDevices())
            {
                using Accelerator accelerator = d.CreateAccelerator(context);
                Console.WriteLine(accelerator);
                Console.WriteLine(GetInfoString(accelerator));
            }

            // Prints all OpenCL accelerators.
            foreach (Device d in context.GetCLDevices())
            {
                using Accelerator accelerator = d.CreateAccelerator(context);
                Console.WriteLine(accelerator);
                Console.WriteLine(GetInfoString(accelerator));
            }
        }

        private static string GetInfoString(Accelerator a)
        {
            StringWriter infoString = new StringWriter();
            a.PrintInformation(infoString);
            return infoString.ToString();
        }
    }
}