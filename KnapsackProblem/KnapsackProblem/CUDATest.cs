using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnapsackProblem
{
    class CUDATest
    {

        public CUDATest()
        {

            using Context context = Context.Create(builder => builder.Cuda());
            using Accelerator accelerator = context.GetCudaDevice(0).CreateAccelerator(context);

            Console.WriteLine(accelerator.Device);
            CreateChromosome(accelerator);
        }


        public void CreateChromosome(Accelerator accelerator)
        {
            var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView<int>, ArrayView<int>, ArrayView<int>>(CreateChromoshomeKernel);
            var items = accelerator.Allocate1D<int>(3);
            var chromoshome = accelerator.Allocate1D<int>(3);
            var randoms = accelerator.Allocate1D<int>(3);
            
            var acceleratorStream = accelerator.DefaultStream;

            ArrayView<byte> itemsArray = new ArrayView<byte>(items,0,3);
            int[] booleans = { 0, 1, 1 };

            items.CopyFrom(acceleratorStream, (ArrayView<byte>)itemsArray, items.Extent);
            randoms.CopyFrom(acceleratorStream, new ArrayView<byte>(randoms,0,3), randoms.Extent);

            kernel(3, (ArrayView<int>)items, (ArrayView<int>)randoms, (ArrayView<int>)chromoshome);
            accelerator.Synchronize();

            var chromhosome = chromoshome.GetAsArray1D();

            for (int i = 0; i < chromoshome.Length; i++)
            {
                Console.WriteLine(chromhosome[i]);
            }

        }

       
        private static void CreateChromoshomeKernel(Index1D index,ArrayView<int> items,ArrayView<int> randoms,ArrayView<int> chromoshome)
        {
             chromoshome[index] = randoms[index];   
        }
    }
}
