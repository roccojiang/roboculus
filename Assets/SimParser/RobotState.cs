using System;
using System.Collections.Generic;
using System.Linq;

namespace SimParser {
/// <summary>
/// A representation of a robot's current state.
/// </summary>
// First assumption: all data are doubles.
public struct RobotState {
  public int Joints { get; private set; }

  public (double x, double y, double z) Position { get; private set; }

  public (double i, double j, double k) Orientation { get; private set; }

  public (double lvx, double lvy, double lvz) LinearVelocity { get;
                                                               private set; }

  public (double avx, double avy, double avz) AngularVelocity { get;
                                                                private set; }

  // All of these must be the same length.
  public List<double> JointPositions { get; private set; }

  public List<double> JointVelocities { get; private set; }

  // Cached double parser, so the conversion isn't done repeatedly.
  private static readonly Parser<double> DoubleParser =
      Scanner.ConvertToParser<double>(double.TryParse);

  // The parser for robot states.
  private static Either<string, double>
  ParseDouble(Scanner scn) => scn.ParseNext(DoubleParser);

  private static Either<string, (double x, double y, double z)>
  ParseCoordTriple(Scanner scn) =>
      ParseDouble(scn)
          .FlatMap(x => ParseDouble(scn).Map(y => (x, y)))
          .FlatMap(coords => ParseDouble(scn).Map(z => (coords.x, coords.y,
                                                        z)));

  public override string ToString() {
    return $"RobotState [{nameof(Joints)}: {Joints}, {nameof(Position)}: {Position}, {nameof(Orientation)}: {Orientation}, {nameof(LinearVelocity)}: {LinearVelocity}, {nameof(AngularVelocity)}: {AngularVelocity}, {nameof(JointPositions)}: {string.Join(", ", JointPositions)}, {nameof(JointVelocities)}: {string.Join(", ", JointVelocities)}]";
  }

  /// <summary>
  /// Checks if the state is valid by checking the joint position bounds.
  /// </summary>
  /// <returns>Either a FormatException or the current instance.</returns>
  public Either<FormatException, RobotState> IsProbablyValid() {
    // For a state to be valid, all of the joint positions must be between -PI/2
    // and PI/2.
    return JointPositions.Any(d => d is<-Math.PI / 2 or> Math.PI / 2)
               ? Either<FormatException, RobotState>.ToLeft(new FormatException(
                     "Joint position is out of range, joint count is probably incorrect."))
               : Either<FormatException, RobotState>.ToRight(this);
  }

  /// <summary>
  /// Parse a robot's state from a scanner, given the number of joints it has to
  /// have.
  /// </summary>
  /// <param name="joints">Number of joints the robot has.</param>
  /// <param name="scn">The stream Scanner to read the state from.</param>
  /// <returns>A robot state, or a token error otherwise.</returns>
  // According to hexapod_env.py, the ordering should be...
  public static Either<string, RobotState> ParseRobotState(int joints,
                                                           Scanner scn) {
    return scn.ParseManyToLF(DoubleParser).FlatMap(ed => {
      // Pre-initialise the robot state object.
      RobotState state = new() { Joints = joints };

      // Check if there are enough doubles.
      List<double> doubles = ed.ToList();

      if (doubles.Count <= 0) {
        return Either<string, RobotState>.ToLeft("End of stream.");
      } else if (doubles.Count != 12 + 2 * joints) {
        int real = (doubles.Count - 12) / 2;
        throw new FormatException(
            $"This data is for a robot with the incorrect joint count! Expected data for {joints} joints, got data for {real} joints!");
      }

      // Otherwise, reinitialise everything.
      state.Position = (doubles[0], doubles[1], doubles[2]);
      state.Orientation = (doubles[3], doubles[4], doubles[5]);
      state.LinearVelocity = (doubles[6], doubles[7], doubles[8]);
      state.AngularVelocity = (doubles[9], doubles[10], doubles[11]);
      state.JointPositions = doubles.GetRange(12, joints);
      state.JointVelocities = doubles.GetRange(12 + joints, joints);

      return Either<string, RobotState>.ToRight(state);
    });
  }
}
}
