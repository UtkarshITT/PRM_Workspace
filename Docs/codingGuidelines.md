# Coding Guidelines

This document defines development standards for this codebase. These guidelines prioritize readability,
maintainability, and long-term scalability.

## 1. Naming

- All names must be intention-revealing, pronounceable, and searchable.
- Avoid abbreviations, encodings, prefixes, and noise words.
- Single-letter names are only acceptable for short-loop iteration variables (`i`, `j`, `k`).
- Files and classes: UpperCamelCase.
- Methods, variables, parameters, and fields: lowerCamelCase.
- Constants (`static final`): UPPER_SNAKE_CASE.
- Packages: all lowercase.
- Do not use names that differ only in subtle ways.
- Pick one word per concept and use it consistently.

## 2. Functions

- Keep functions small, ideally under 20 lines.
- A function should do one thing, at one level of abstraction.
- Use descriptive names; a long clear name beats a short cryptic one.
- Minimize arguments: zero is ideal, three or more should be encapsulated in an object.
- No boolean flag arguments; split into two clearly named functions.
- No side effects; a function should only do what its name promises.
- Do not repeat yourself (DRY).

## 3. Comments

- Comments are a last resort; prefer self-explanatory code over comments.
- Acceptable comments:
  - Legal notices.
  - Explanation of intent.
  - Clarification.
  - Warnings.
  - TODOs.
  - Javadoc on public APIs.
- Avoid:
  - Redundant comments.
  - Journal/changelog comments.
  - Closing-brace comments.
  - Commented-out code.
  - Attribution comments.
  - Positional markers.
- Comments for a variable, class, or function are placed before the commented component.

## 4. Formatting

### Vertical

- Use blank lines as paragraph separators between methods, field groups, and logical sections within a method.
- Related code stays close together; unrelated code is separated.
- Caller methods should appear above the methods they call (Stepdown Rule).
- Keep files under 500 lines.
- Every file ends with exactly one trailing newline.

### Horizontal

- Keep lines under 120 characters.
- Indent each nested block with one tab.
- One space around most operators (`x = y + z`), except `.`, `++`, `--`.
- One space before every opening `{`.
- No space between a function name and its argument list: `doWork(arg)`, not `doWork (arg)`.
- Always use braces for `if` / `for` / `while`, even single-line bodies.

## 5. Classes

- One top-level class per file; file name matches the class name.
- Each class has a single responsibility, describable without "and" or "or."
- Fields are private; expose behavior through well-named methods, not raw data.
- Aim for high cohesion: most methods use most instance variables.
- Class member variables and methods follow lowerCamelCase.
- Organize members top-to-bottom:
  - Constants.
  - Static fields.
  - Instance fields.
  - Constructors.
  - Public methods.
  - Private methods.

## 6. Error Handling

- Use exceptions, not error return codes.
- Provide meaningful context in exception messages.
- Do not return `null`; return empty collections or `Optional` instead.
- Do not pass `null` as an argument unless an API explicitly requires it.
- Use unchecked exceptions to avoid leaking implementation details through the call chain.

## 7. Objects and Data Structures

- Objects hide data and expose behavior; data structures expose data and have no behavior. Do not create hybrids.
- Follow the Law of Demeter: a method should only talk to its immediate friends, not strangers down a chain of calls.

## 8. SOLID Principles

- Single Responsibility: one reason to change per class.
- Open / Closed: open for extension, closed for modification.
- Liskov Substitution: subtypes must be substitutable for their base types.
- Interface Segregation: many small interfaces over one large one.
- Dependency Inversion: depend on abstractions, not concretions.

## 9. Code Smells to Eliminate

- Dead code: delete, do not comment out.
- Magic numbers: replace with named constants.
- Feature envy: move the method to the class whose data it uses most.
- Primitive obsession: use domain types instead of raw primitives.
- Long parameter lists: encapsulate in an object.
- Divergent change / shotgun surgery: rebalance responsibilities.
- Inappropriate intimacy: classes should not dig into each other's internals.