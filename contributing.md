# How to contribute

## Submitting changes

Please send a pull request with a clear list of what you've done (read
more about [pull requests](https://help.github.com/articles/about-pull-requests/).
Please follow our coding conventions (below) and make sure all of your
commits are atomic (one feature per commit).

Always write a clear log message for your commits. One-line messages are
fine for small changes, but bigger changes should look like this:

    $ git commit -m "A brief summary of the commit
    > 
    > A paragraph describing what changed and its impact."

## Coding conventions

The coding conventions are all defined in [.editorconfig](.editorconfig)
and [Rebracer.xml](Rebracer.xml) so installing the
[EditorConig](http://editorconfig.org/) and
[Rebracer](https://github.com/SLaks/Rebracer) plugins for visual studio
will magically keep your changes consistent with the rest of the code.

The existing code should provide all the guidance needed, but here's a
brief summary:

* use tabs for indentation, not spaces
* don't use newlines before braces
* use line continuation for lines over 80 chars

