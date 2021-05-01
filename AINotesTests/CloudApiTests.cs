using AINotesCloud;
using Xunit;

namespace AINotesTests {
    public class CloudApiTests {
        private readonly CloudApi _cloudApi = new CloudApi("https://srv1.vincentscode.de/");
        
        // facts
        [Fact]
        public void DummyFact() {
            Assert.True(true);
        }

        // theories
        [Theory]
        [InlineData("https://srv1.vincentscode.de/", true)]
        [InlineData("https://srv1.vincentscode.de", true)]
        [InlineData("srv1.vincentscode.de/", true)]
        [InlineData("srv1.vincentscode.de", true)]
        [InlineData("http://srv1.vincentscode.de/", false)]
        [InlineData("test", false)]
        [InlineData("https://www.google.com/", false)]
        [InlineData("https://www.kahsdfkjsadhgfjasdhfjkasdhfkjlahsdjkfhasdkjfhasjkdfgi7dtfasikjdfbasjmdfv.de", false)]
        public async void ConnectTest(string url, bool expected) {
            var testCloudApi = new CloudApi(url);
            var (actual, _) = await testCloudApi.Login("test@gmail.com", "asdpf97sodfasdmfo");
            Assert.Equal(expected, actual);
        }
        
        [Theory]
        [InlineData("test", "test", false)]
        [InlineData("test@gmail.com", "test", false)]
        [InlineData("asdfkjasdflkjöasldfjasdfkj", "asdfkjasf30q4901CX##test", false)]
        public async void RegisterTest(string email, string password, bool expected) {
            var (actual, _) = await _cloudApi.RegisterAndLogin("Display Name", email, password);
            Assert.Equal(expected, actual);
        }
    }
}