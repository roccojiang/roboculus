using System;
using System.Collections.Generic;

namespace SimParser {
/// <summary>
/// Either sum type for easier error handling.
/// </summary>
/// <typeparam name="L">Left (usually error) type.</typeparam>
/// <typeparam name="R">Right (usually success / result) type.</typeparam>
public abstract class Either<L, R> {
  /// <summary>
  /// Get a value from a Left.
  /// </summary>
  /// <returns>The item if this is a Left. Throws an exception
  /// otherwise.</returns>
  public abstract L FromLeft();

  /// <summary>
  /// Get a value from a Right.
  /// </summary>
  /// <returns>The item if this is a Right. Throws an exception
  /// otherwise.</returns>
  public abstract R FromRight();

  /// <summary>
  /// Is this a Left?
  /// </summary>
  public abstract bool IsLeft();

  /// <summary>
  /// Is this a Right?
  /// </summary>
  public abstract bool IsRight();

  /// <summary>
  /// Make a Left from an item.
  /// </summary>
  /// <param name="item">Item to hold inside.</param>
  /// <returns>The created Left instance.</returns>
  public static Either<L, R> ToLeft(L item) => new Left<L, R>(item);

  /// <summary>
  /// Make a Right from an item.
  /// </summary>
  /// <param name="item">Item to hold inside.</param>
  /// <returns>The created Right instance.</returns>
  public static Either<L, R> ToRight(R item) => new Right<L, R>(item);

  /// <summary>
  /// Maps the item in an Either, only if this is a Right instance.
  /// It should obey Functor laws.
  /// </summary>
  /// <param name="fn">Mapper function.</param>
  /// <typeparam name="S">Output type.</typeparam>
  /// <returns>The mapped Right if this is a Right, returns a casted Left
  /// otherwise.</returns> <exception cref="InvalidCastException">If the
  /// subclass is not Left or Right.</exception>
  // Considering that R is a phantom type in Left, it should be fine to cast.
  public Either<L, S> Map<S>(Func<R, S> fn) {
    return this switch { Left<L, R> left => new Left<L, S>(left.FromLeft()),
                         Right<L, R> right =>
                             new Right<L, S>(fn(right.FromRight())),
                         _ => throw new InvalidCastException(
                             "This is not a valid Either subclass!") };
  }

  /// <summary>
  /// Lifted application for Either types.
  /// It should obey Applicative Functor laws.
  /// </summary>
  /// <param name="fn">Lifted function in an Either.</param>
  /// <typeparam name="S">Output type.</typeparam>
  /// <returns>The mapped Right if this is a Right, returns a casted Left
  /// otherwise.</returns> <exception cref="InvalidCastException">If the
  /// subclass is not Left or Right.</exception>
  public Either<L, S> Apply<S>(Either<L, Func<R, S>> fn) {
    return (fn, this) switch {
      (Left<L, Func<R, S>> le, not null) => new Left<L, S>(le.FromLeft()),
      (Right<L, Func<R, S>> rf, not null) => Map(rf.FromRight()),
      _ =>
          throw new InvalidCastException("This is not a valid Either subclass!")
    };
  }

  /// <summary>
  /// Maps the item in an Either to another Either instance, only if this is a
  /// Right instance. It should obey Monad laws.
  /// </summary>
  /// <param name="fn">Mapper function.</param>
  /// <typeparam name="S">Output type.</typeparam>
  /// <returns>The output Right if this is a Right, returns a casted Left
  /// otherwise.</returns> <exception cref="InvalidCastException">If the
  /// subclass is not Left or Right.</exception>
  // See comment on FlatMap.
  public Either<L, S> FlatMap<S>(Func<R, Either<L, S>> fn) {
    return this switch { Left<L, R> left => new Left<L, S>(left.FromLeft()),
                         Right<L, R> right => fn(right.FromRight()),
                         _ => throw new InvalidCastException(
                             "This is not a valid Either subclass!") };
  }
}

public static class EitherExtensions {
  // Sequence a list of Either actions together.
  public static Either<L, IEnumerable<R>> Sequence<L, R>(
      this IEnumerable<Either<L, R>> actions) => actions.Traverse(x => x);

  // Traverse over a list, while evaluating Either actions.
  public static Either<L, IEnumerable<R>>
  Traverse<L, R, T>(this IEnumerable<T> inputs, Func<T, Either<L, R>> process) {
    List<R> result = new();

    foreach (T input in inputs) {
      Either<L, R> action = process(input);

      if (action is Left<L, R> left)
        return new Left<L, IEnumerable<R>>(left.FromLeft());
      else if (action is Right<L, R> right)
        result.Add(right.FromRight());
      else
        throw new InvalidCastException("This is not a valid Either subclass!");
    }

    return Either<L, IEnumerable<R>>.ToRight(result);
  }
}

/// <summary>
/// Left type of Either.
/// </summary>
/// <typeparam name="L">Left (usually error) type.</typeparam>
/// <typeparam name="R">Right (usually success / result) type. A phantom type in
/// this case.</typeparam>
public sealed class Left<L, R> : Either<L, R> {
  private L LeftItem { get; }

  public Left(L leftItem) { LeftItem = leftItem; }

  public override L FromLeft() => LeftItem;

  public override
      R FromRight() => throw new InvalidCastException("This is a Left!");

  public override bool IsLeft() => true;

  public override bool IsRight() => false;
}

/// <summary>
/// Right type of Either.
/// </summary>
/// <typeparam name="L">Left (usually error) type. A phantom type in this
/// case.</typeparam> <typeparam name="R">Right (usually success / result)
/// type.</typeparam>
public sealed class Right<L, R> : Either<L, R> {
  private R RightItem { get; }

  public Right(R rightItem) { RightItem = rightItem; }

  public override
      L FromLeft() => throw new InvalidCastException("This is a Right!");

  public override R FromRight() => RightItem;

  public override bool IsLeft() => true;

  public override bool IsRight() => false;
}
}
