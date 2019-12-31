# inedosql

inedosql is a simple command-line tool used to consistently and safely update SQL database schemas. It provides:

 - change tracking
 - run history
 - safety, by preventing change scripts from being rerun, and preventing additional scripts from running after a failure
 - easy redistribution

This utility is used by Inedo's products to deliver database upgrades, but all are welcome to
use and/or contribute to it.

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
