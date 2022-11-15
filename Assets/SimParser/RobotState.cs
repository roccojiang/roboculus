using System;
using System.Collections.Generic;
using System.Linq;

namespace SimParser {
/// <summary>
/// A representation of a robot's current state.
/// </summary>
// First assumption: all data are doubles.
public struct RobotState {
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
    return $"RobotState [{nameof(Position)}: {Position}, {nameof(Orientation)}: {Orientation}, {nameof(LinearVelocity)}: {LinearVelocity}, {nameof(AngularVelocity)}: {AngularVelocity}, {nameof(JointPositions)}: {string.Join(", ", JointPositions)}, {nameof(JointVelocities)}: {string.Join(", ", JointVelocities)}]";
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
    // Pre-initialise the robot state object.
    RobotState state = new() { JointPositions = new List<double>(),
                               JointVelocities = new List<double>() };

    // Parse state components one-by-one and insert into local copy of state.
    return ParseCoordTriple(scn)
        .FlatMap(pos => {
          state.Position = pos;
          return ParseCoordTriple(scn);
        })
        .FlatMap(ori => {
          state.Orientation = ori;
          return ParseCoordTriple(scn);
        })
        .FlatMap(lv => {
          state.LinearVelocity = lv;
          return ParseCoordTriple(scn);
        })
        .FlatMap(av => {
          state.AngularVelocity = av;
          return Enumerable.Range(0, joints)
              .Select(
                  _ => ParseDouble(scn))
              .Sequence();
        })
        .FlatMap(jps => {
          state.JointPositions.AddRange(jps);
          return Enumerable.Range(0, joints)
              .Select(
                  _ => ParseDouble(scn))
              .Sequence();
        })
        .FlatMap(jvs => {
          state.JointVelocities.AddRange(jvs);
          return Either<string, RobotState>.ToRight(state);
        });
  }
}
}
