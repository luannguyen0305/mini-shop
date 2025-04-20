using Xunit;

namespace Shop.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void TestAddition()
        {
            // Arrange: Khởi tạo dữ liệu
            int a = 2;
            int b = 3;
            int expected = 5;

            // Act: Thực hiện phép cộng
            int result = a + b;

            // Assert: Kiểm tra kết quả
            Assert.Equal(expected, result);
        }
    }
}
