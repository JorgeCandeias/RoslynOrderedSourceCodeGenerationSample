namespace Tests
{
    public class GeneratorUnitTests
    {
        [Fact]
        public void Test1()
        {
            // asset model processed by first generator
            Assert.Equal(1, UserModelA.A);

            // assert model processed by second generator
            Assert.Equal(2, UserModelB.B);

            // assert model processed by first generator and then second generator
            Assert.Equal(2, UserModelAB.B);
        }
    }
}