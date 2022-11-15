using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace SimParser {
/// <summary>
/// A lazy parser through a simulation file.
/// </summary>
public class SimulationParser : IEnumerable<RobotState>, IDisposable {
  private int Joints { get; }

  private Stream FileStream { get; }

  private bool Consumed { get; set; }

  private bool Continuous { get; set; }

  /// <summary>
  /// Create a simulation parser iterable for an n-jointed robot.
  /// </summary>
  /// <param name="joints">The number of joints the robot has.</param>
  /// <param name="fileStream">The file stream to read from.</param>
  /// <param name="continuous">Should the parser stop if it fails to
  /// parse?</param>
  public SimulationParser(int joints, Stream fileStream,
                          bool continuous = false) {
    Joints = joints;
    FileStream = fileStream;
    Continuous = continuous;
  }

  /// <summary>
  /// Create a simulation parser iterable for an n-jointed robot.
  /// </summary>
  /// <param name="joints">The number of joints the robot has.</param>
  /// <param name="filePath">The file path of the stream to read from.</param>
  /// <param name="continuous">Should the parser stop if it fails to
  /// parse?</param>
  public SimulationParser(int joints, string filePath, bool continuous = false)
      : this(joints, File.OpenRead(filePath), continuous) {}

  public IEnumerator<RobotState> GetEnumerator() {
    // Make sure this method is only invoked once.
    if (Consumed)
      throw new IOException("Filestream has already been consumed.");
    Consumed = true;

    // Consume all robot states from file.
    Scanner fileScanner = new(FileStream);
    Either<string, RobotState> result =
        RobotState.ParseRobotState(Joints, fileScanner);

    while (Continuous || !result.IsLeft()) {
      if (result.IsRight())
        yield return result.FromRight();
      result = RobotState.ParseRobotState(Joints, fileScanner);
    }
  }

  IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

  public void Dispose() {
    // Halt continuous mode parsing, and dispose of the stream used.
    Continuous = false;
    FileStream?.Dispose();
  }
}
}
