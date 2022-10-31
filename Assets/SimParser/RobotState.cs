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

  public (double i, double j, double k, double w) Orientation { get;
                                                                private set; }

  public (double lvx, double lvy, double lvz) LinearVelocity { get;
                                                               private set; }

  public (double avx, double avy, double avz) AngularVelocity { get;
                                                                private set; }

  // All of these must be the same length.
  public List<(double x, double y, double z)> JointPositions { get;
                                                               private set; }

  public List<(double vx, double vy, double vz)> JointVelocities {
      get; private set; }

  // The parser for robot states.
  private static IEither<string, double>
  ParseDouble(Scanner scn) => scn.ParseNext<double>(double.TryParse);

  private static IEither<string, (double x, double y, double z)>
  ParseCoordTriple(Scanner scn) =>
      ParseDouble(scn)
          .FlatMap(x => ParseDouble(scn).Map(y => (x, y)))
          .FlatMap(coords => ParseDouble(scn).Map(z => (coords.x, coords.y,
                                                        z)));

  private static IEither<string, (double i, double j, double k, double w)>
  ParseCoordQuadruple(Scanner scn) => ParseCoordTriple(scn).FlatMap(
      coords => ParseDouble(scn).Map(w => (coords.x, coords.y, coords.z, w)));

  /// <summary>
  /// Parse a robot's state from a scanner, given the number of joints it has to
  /// have.
  /// </summary>
  /// <param name="joints">Number of joints the robot has.</param>
  /// <param name="scn">The stream Scanner to read the state from.</param>
  /// <returns>A robot state, or a token error otherwise.</returns>
  // According to hexapod_env.py, the ordering should be...
  internal static IEither<string, RobotState> ParseRobotState(int joints,
                                                              Scanner scn) {
    // Pre-initialise the robot state object.
    RobotState state = new RobotState {
      JointPositions = new List<(double x, double y, double z)>(),
      JointVelocities = new List<(double vx, double vy, double vz)>()
    };

    return ParseCoordTriple(scn)
        .FlatMap(pos => {
          state.Position = pos;
          return ParseCoordQuadruple(scn);
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
                  _ => ParseCoordTriple(scn))
              .Sequence();
        })
        .FlatMap(jps => {
          state.JointPositions.AddRange(jps);
          return Enumerable.Range(0, joints)
              .Select(
                  _ => ParseCoordTriple(scn))
              .Sequence();
        })
        .FlatMap(jvs => {
          state.JointVelocities.AddRange(jvs);
          return IEither<string, RobotState>.ToRight(state);
        });
  }
}

}
