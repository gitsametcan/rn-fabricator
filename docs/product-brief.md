# Product Brief

## Name

rn-fabricator

## Summary

rn-fabricator is a .NET based CLI tool for developers who build mobile apps with React Native CLI. It reduces repetitive project setup work by checking the local development environment, creating new React Native projects, applying starter templates, and generating example configuration files.

## Problem

React Native CLI projects often require the same manual setup steps:

- Checking Node.js, npm, Git, Watchman, Xcode, CocoaPods, Java, and Android SDK.
- Creating a consistent folder structure.
- Adding repeated starter screens and authentication flow.
- Preparing environment example files.
- Running release readiness checks before distribution.

These steps are error-prone, time-consuming, and easy to perform inconsistently across projects.

## Target Users

- React Native CLI developers.
- Mobile developers who frequently start new projects.
- Teams that want repeatable project setup conventions.
- Developers who want a portfolio-friendly, transparent CLI tool they can extend.

## Goals

- Provide a fast, predictable way to start React Native CLI projects.
- Detect common local environment problems before project creation.
- Provide reusable templates for common mobile app foundations.
- Keep the tool understandable, testable, and easy to extend.

## Non-Goals

- rn-fabricator will not replace React Native CLI.
- rn-fabricator will not be a visual app builder in the MVP.
- rn-fabricator will not manage app store publishing in the MVP.
- rn-fabricator will not support Expo as a first-class target in the MVP.

## Success Criteria

- A developer can run `rn-fabricator doctor` and understand what is missing from their machine.
- A developer can run `rn-fabricator create` and get a working React Native CLI project.
- A developer can apply the `basic-auth` template without manually copying files.
- The project has clear documentation, tests, and CI from the beginning.
