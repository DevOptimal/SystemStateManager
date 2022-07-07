# System State Manager

## What does it do?

The System State Manager creates snapshots of the state of supported system resources at a given point in time which can be used to restore the state of those system resources at a later point in time.

## Why do I need it?

This is particularly useful in automation that needs to make temporary changes to the state of the system it runs on, but is expected to leave the system in the same state it was in before the automation ran.

## How about an example?

For example, during automated regression testing, sometimes a test needs to configure the product under test by changing system resources (such as an environment variable, configuration file, or registry key). Other times, the product will change system resources itself. In either case, the altered state of those system resources can interfere with subsequent tests, leading to failures that are difficult to debug.

This is where the System State Manager comes in: Prior to executing the test, the System State Manager can be used to snapshot the state of any system resources that the test or product might alter. During execution of the test, those system resources can be altered however needed. At the completion of the test, the System State Manager can be used to restore those system resources to the state they were in when snapshotted at the beginning of the test.
