using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using OnsetDetection;
using System.IO;
using MathNet.Numerics.LinearAlgebra;

namespace OnsetDetection_tests
{
    public class FilterTests
    {
        [Fact]
        public void TestFilterBankProduction()
        {
            var filterbankData = new Filter(1024, 44100, new MemoryAllocator()).Filterbank.ToRowWiseArray();
            //load test data
            var data = File.ReadAllLines("../../FilterbankData.csv").Select(a => float.Parse(a)).ToArray();
            for (int i = 0; i < data.Length; i++)
            {
                Assert.True(Math.Abs(filterbankData[i] - data[i]) < 0.001);
            }
        }
    }

    public class CompatibilityTests
    {
        [Fact]
        public void TestUniformFilter()
        {
            //load test data
            var input = File.ReadAllLines("../../UniformFilterInput.csv").Select(a => float.Parse(a)).ToArray();
            var expectedOutput1 = File.ReadAllLines("../../UniformFilterOutput1.csv").Select(a => float.Parse(a)).ToArray();
            var expectedOutput2 = File.ReadAllLines("../../UniformFilterOutput2.csv").Select(a => float.Parse(a)).ToArray();
            var expectedOutput3 = File.ReadAllLines("../../UniformFilterOutput3.csv").Select(a => float.Parse(a)).ToArray();
            var expectedOutput4 = File.ReadAllLines("../../UniformFilterOutput4.csv").Select(a => float.Parse(a)).ToArray();
            Matrix<float> matrixInput = Matrix<float>.Build.DenseOfRowArrays(input);
            Vector<float> vectorInput = Vector<float>.Build.DenseOfArray(input);

            //test1: size=2, origin=0
            var test = SciPyCompatibility.UniformFilter1D(matrixInput, 2, 0);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput1[i] - test[0, i]) < 0.001);
            }

            //test2: size=5, origin=0
            test = SciPyCompatibility.UniformFilter1D(matrixInput, 5, 0);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput2[i] - test[0, i]) < 0.001);
            }
            //test3: size=5, origin=-2
            test = SciPyCompatibility.UniformFilter1D(matrixInput, 5, -2);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput3[i] - test[0, i]) < 0.001);
            }
            //test4: size=5, origin=2
            test = SciPyCompatibility.UniformFilter1D(matrixInput, 5, 2);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput4[i] - test[0, i]) < 0.001);
            }

            //test vector routines
            //test1: size=2, origin=0
            var vtest = SciPyCompatibility.UniformFilter1D(vectorInput, 2, 0);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput1[i] - vtest[i]) < 0.001);
            }

            //test2: size=5, origin=0
            vtest = SciPyCompatibility.UniformFilter1D(vectorInput, 5, 0);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput2[i] - vtest[i]) < 0.001);
            }
            //test3: size=5, origin=-2
            vtest = SciPyCompatibility.UniformFilter1D(vectorInput, 5, -2);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput3[i] - vtest[i]) < 0.001);
            }
            //test4: size=5, origin=2
            vtest = SciPyCompatibility.UniformFilter1D(vectorInput, 5, 2);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput4[i] - vtest[i]) < 0.001);
            }
        }

        [Fact]
        public void TestMaximumFilter()
        {
            //load test data
            var input = File.ReadAllLines("../../MaximumFilterInput.csv").Select(a => float.Parse(a)).ToArray();
            var expectedOutput1 = File.ReadAllLines("../../MaximumFilterOutput1.csv").Select(a => float.Parse(a)).ToArray();
            var expectedOutput2 = File.ReadAllLines("../../MaximumFilterOutput2.csv").Select(a => float.Parse(a)).ToArray();
            var expectedOutput3 = File.ReadAllLines("../../MaximumFilterOutput3.csv").Select(a => float.Parse(a)).ToArray();
            var expectedOutput4 = File.ReadAllLines("../../MaximumFilterOutput4.csv").Select(a => float.Parse(a)).ToArray();
            Matrix<float> matrixInput = Matrix<float>.Build.DenseOfRowArrays(input);
            Vector<float> vectorInput = Vector<float>.Build.DenseOfArray(input);

            //test matrix routines
            //test1: size=2, origin=0
            var test = SciPyCompatibility.MaximumFilter1D(matrixInput, 2, 0);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput1[i] - test[0, i]) < 0.001);
            }

            //test2: size=5, origin=0
            test = SciPyCompatibility.MaximumFilter1D(matrixInput, 5, 0);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput2[i] - test[0, i]) < 0.001);
            }
            //test3: size=5, origin=-2
            test = SciPyCompatibility.MaximumFilter1D(matrixInput, 5, -2);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput3[i] - test[0, i]) < 0.001);
            }
            //test4: size=5, origin=2
            test = SciPyCompatibility.MaximumFilter1D(matrixInput, 5, 2);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput4[i] - test[0, i]) < 0.001);
            }

            //test vector routines
            //test1: size=2, origin=0
            var vtest = SciPyCompatibility.MaximumFilter1D(vectorInput, 2, 0);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput1[i] - vtest[i]) < 0.001);
            }

            //test2: size=5, origin=0
            vtest = SciPyCompatibility.MaximumFilter1D(vectorInput, 5, 0);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput2[i] - vtest[i]) < 0.001);
            }
            //test3: size=5, origin=-2
            vtest = SciPyCompatibility.MaximumFilter1D(vectorInput, 5, -2);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput3[i] - vtest[i]) < 0.001);
            }
            //test4: size=5, origin=2
            vtest = SciPyCompatibility.MaximumFilter1D(vectorInput, 5, 2);
            for (int i = 0; i < matrixInput.ColumnCount; i++)
            {
                Assert.True(Math.Abs(expectedOutput4[i] - vtest[i]) < 0.001);
            }
        }
    }
}
