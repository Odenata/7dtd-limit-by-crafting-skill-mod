def _nuget_repo_impl(ctx):
    ctx.download(
        url = "https://dist.nuget.org/win-x86-commandline/v6.11.1/nuget.exe",
        output = "file/nuget.exe",
        sha256 = "c0ddc9cb0633c4607da7e8028eb4f91248c8b74e45a68b0c79fcfa7d78c2a481",
        executable = True,
    )
    ctx.file("file/BUILD.bazel", 'exports_files(["nuget.exe"])\n')

nuget_repo = repository_rule(
    implementation = _nuget_repo_impl,
)
