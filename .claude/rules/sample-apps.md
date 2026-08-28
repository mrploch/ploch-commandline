# Sample Application Standards

## Scope

These rules apply when making changes to repositories that contain sample or showcase applications.

## ploch-data SampleApp

The `ploch-data` repository contains a `SampleApp` that showcases `Ploch.Data` library features.

### Rules

- **Keep it current:** When adding new features or modifying existing features in the `ploch-data` library, update the `SampleApp` to demonstrate them.
- **Must compile and run:** The SampleApp must work. Always test it manually after making changes to the library or the SampleApp itself.
- **Must work in isolation:** The SampleApp **must** use NuGet package references, not `ProjectReference`. There must be no reference to any project in the workspace. Someone should be able to copy the SampleApp to a different machine and run it without the rest of the workspace.
- **Manual test at the end:** After all automated tests pass, run the SampleApp and verify it works end-to-end. This is a mandatory step, not optional.

## General SampleApp Rules

For any repository containing a sample or showcase application:

- Sample applications exist to demonstrate library features to consumers. They must be realistic and useful.
- Always verify sample apps work after library changes, even if the sample app code was not directly modified — API changes can break consumers.
- Sample apps should demonstrate idiomatic usage, not internal implementation details.
