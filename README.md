# inedosql

inedosql is a simple command-line tool used to consistently and safely update SQL database schemas. It provides:

 - change tracking
 - run history
 - safety, by preventing change scripts from being rerun, and preventing additional scripts from running after a failure
 - easy redistribution

This utility is used by Inedo's products to deliver database upgrades, but all are welcome to
use and/or contribute to it.

## Tracked and Untracked SQL Scripts

inedosql is intended for use with lexically-ordered scripts in `.sql` files. When using the `update` command
(see below), it will iterate all `.sql` files in a directory recursively, possibly executing it against
the target database depending on whether it is a tracked or untracked script.

A **tracked** script, is a `.sql` script with a special comment header on the first line using the following format:

    --AH:ScriptId=<unique-guid>[;ExecutionMode=<Once|OnChange|Always>]

Each tracked script's GUID uniquely identifies it and allows its executions against a target database to
be recorded. This allows one-time database schema changes to be executed once and only once against a database.
The `ExecutionMode` value is optional, and may be one of:
 - **Once**: The script is only ever run once. This is the default behavior if `ExecutionMode` is not specified.
 - **OnChange**: The script is run only when it has changed compared to the last time it was executed against the target database.
 - **Always**: The script is run every time like an untracked script, but the success/failure of its last run is retained because it is tracked.

An **untracked** script is simple a plain `.sql` script without the header. These scripts are run every time,
and nothing is persisted about each run.

### Examples of Tracked Scripts

This uses the implicit `ExecutionMode=Once` to ensure that the `[MyTable]` table
is only created one time:

    --AH:ScriptId=E632420E-0D90-46F7-A23E-1F374786CABD
    CREATE TABLE [MyTable] ([MyColumn] INT)

An example of a "cleanup script" to remove old views. It can be run multiple times, but doesn't need to be
re-run unless it's been modified, so it uses `ExecutionMode=OnChange`:    

    --AH:ScriptId=5428C9CD-59DD-40B7-BF39-87259CDF7653;ExecutionMode=OnChange
    IF OBJECT_ID('MyOldView') IS NOT NULL DROP VIEW [MyOldView]

## Command Line Reference

    inedosql <command> [--connection-string=<connection-string>] [options...]

`<connection-string>` is required, and must contain a valid SQL Server connection string that specifies a database.

`<command>` must be one of:

### update

    inedosql update <script-path> [--force]

Executes all `.sql` files in `<script-path>`, recursively iterating through all subdirectories.
Scripts are executed in lexical order.

When a script execution fails, no further scripts will be executed; inedosql will terminate immediately.
If a tracked script fails, the error is recorded and future `update` commands executed against the database
will fail until the error has been resolved or unless the `--force` option is specified.

### errors

    inedosql errors [--all]

Lists all tracked scripts with unresolved errors. If `--all` is specified, tracked scripts with resolved
errors are also displayed.

### error

    inedosql error <script-guid-or-name>

Displays details about a tracked script execution error. `<script-guid-or-name>` may be either the
tracked script's unique ID or its file name relative to the execution directory. Note that script names
are not guaranteed to be unique; if there are multiple scripts executed of the same name, specifying
the name will have undefined results.

### script

    inedosql script <script-guid-or-name>

Displays details about a tracked script execution, including the executed script itself if it was stored.
`<script-guid-or-name>` may be either the tracked script's unique ID or its file name relative to
the execution directory. Note that script names are not guaranteed to be unique; if there are multiple
scripts executed of the same name, specifying the name will have undefined results.

### resolve-error

    inedosql resolve-error [<script-guid>] [--all] [--comment=<comment>]

Marks one or more tracked script execution errors as resolved, which will allow the `update` command
to execute scripts without specifying the `--force` option.

If `<script-guid>` is specified, this command applies to a recorded error of a specific tracked
script, and `<script-guid>` must be the unique ID of the script.

If `--all` is specified, this command applies to all unresolved tracked script errors. Specifying
`<script-guid>` and `--all` together is invalid.

`<comment>` is optional, and if specified, provides a short comment to be recorded with the
resolution for the script.
