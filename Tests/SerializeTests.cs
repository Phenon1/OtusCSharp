using OtusCSharpModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
  
    public class SerializeTests
    {

        [Fact]
        public void UserProfileTest()
        {
            UserProfile test1 = new UserProfile("test1Name");
            test1.CreatedOn = DateTime.Now;
            test1.Id = 1;
            UserProfile test2 = new UserProfile("test2Name");

            using MemoryStream stream1 = new MemoryStream();
            test1.SerializeToBinary(stream1);
            using MemoryStream stream2 = new MemoryStream();
            test2.SerializeToBinary(stream2);

            stream1.Position = 0;
            stream2.Position = 0;

            var desTest1 = UserProfile.DeserializeFromBinary(stream1);
            var desTest2 = UserProfile.DeserializeFromBinary(stream2);

            Assert.Equal(test1, desTest1);
            Assert.Equal(test2, desTest2);



        }
    }
}
