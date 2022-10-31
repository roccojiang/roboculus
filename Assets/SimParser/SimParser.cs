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

  /// <summary>
  /// Create a simulation parser iterable for an n-jointed robot.
  /// </summary>
  /// <param name="joints">The number of joints the robot has.</param>
  /// <param name="fileStream">The file stream to read from.</param>
  public SimulationParser(int joints, Stream fileStream) {
    Joints = joints;
    FileStream = fileStream;
  }

  /// <summary>
  /// Create a simulation parser iterable for an n-jointed robot.
  /// </summary>
  /// <param name="joints">The number of joints the robot has.</param>
  /// <param name="filePath">The file path of the stream to read from.</param>
  public SimulationParser(int joints, string filePath)
      : this(joints, File.OpenRead(filePath)) {}

  public IEnumerator<RobotState> GetEnumerator() {
    // Make sure this method is only invoked once.
    if (Consumed)
      throw new IOException("Filestream has already been consumed.");
    Consumed = true;

    // Consume all robot states from file.
    Scanner fileScanner = new Scanner(FileStream);
    Either<string, RobotState> result =
        RobotState.ParseRobotState(Joints, fileScanner);

    while (!result.IsLeft()) {
      yield return result.FromRight();
      result = RobotState.ParseRobotState(Joints, fileScanner);
    }
  }

  IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

  public void Dispose() { FileStream?.Dispose(); }
}
}
