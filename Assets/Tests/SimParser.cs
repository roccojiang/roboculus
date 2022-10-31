using SimParser;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace Tests {
public class SimTestScript {
  // Test to see if the robot parser actually works.
  [Test]
  public void TestRobotParser() {
    // The hexapod with 18 joints should have 48 inputs:
    // +3 for position
    // +3 for orientation
    // +3 for lin. velocity
    // +3 for ang. velocity
    // +18 for joint positions
    // +18 for joint velocities
    string testLine =
        "1.0 -2.0 3.0 4.0 -5.0 6.0 7.0 -8.0 9.0 10.0 -11.0 12.0 " +
        "13.0 14.0 15.0 16.0 17.0 18.0 19.0 20.0 21.0 22.0 23.0 24.0 " +
        "25.0 26.0 27.0 28.0 29.0 30.0 31.0 32.0 33.0 34.0 35.0 36.0 " +
        "37.0 38.0 39.0 40.0 41.0 42.0 43.0 44.0 45.0 46.0 47.0 48.0";

    Stream testLineStream = new MemoryStream(Encoding.UTF8.GetBytes(testLine));
    Scanner testScanner = new(testLineStream);

    // Actually do the parsing.
    Either<string, RobotState> parseResult =
        RobotState.ParseRobotState(18, testScanner);

    // Check if the result has parsed.
    if (parseResult is Left<string, RobotState> left) {
      Assert.Fail("Parse has failed! Reason: " + left.FromLeft());
    }

    // Now check if all the values are correct.
    RobotState foundState = parseResult.FromRight();

    Assert.AreEqual(foundState.Position, (1.0, -2.0, 3.0));
    Assert.AreEqual(foundState.Orientation, (4.0, -5.0, 6.0));
    Assert.AreEqual(foundState.LinearVelocity, (7.0, -8.0, 9.0));
    Assert.AreEqual(foundState.AngularVelocity, (10.0, -11.0, 12.0));

    for (int i = 13; i <= 30; ++i) {
      Assert.AreEqual(foundState.JointPositions[i - 13], (double)i);
    }

    for (int i = 31; i <= 48; ++i) {
      Assert.AreEqual(foundState.JointVelocities[i - 31], (double)i);
    }

    // MemoryStreams don't need to be closed, but just in case...
    testLineStream.Close();
  }
}
}
