def _simple_nuget_repo_impl(ctx):
    ctx.download_and_extract(
        url = "https://www.nuget.org/api/v2/package/{}/{}".format(ctx.attr.package, ctx.attr.version),
        output = "",
        sha256 = ctx.attr.sha256,
        type = "zip",
    )
    ctx.file(
        "BUILD.bazel",
        """package(default_visibility = ["//visibility:public"])

load("@rules_dotnet_framework//dotnet:defs.bzl", "net_import_library")

filegroup(
    name = "files",
    srcs = glob([
        "**/*.dll",
    ]),
)

_all_dlls = glob(
    ["**/*.dll"],
    exclude = ["**/ref/**", "**/resources/**"],
)

_net_dlls = glob(["lib/net472/*.dll"], allow_empty = True) or \\
            glob(["lib/net471/*.dll"], allow_empty = True) or \\
            glob(["lib/net47/*.dll"], allow_empty = True) or \\
            glob(["lib/net462/*.dll"], allow_empty = True) or \\
            glob(["lib/net461/*.dll"], allow_empty = True) or \\
            glob(["lib/net46/*.dll"], allow_empty = True) or \\
            glob(["lib/net452/*.dll"], allow_empty = True) or \\
            glob(["lib/net451/*.dll"], allow_empty = True) or \\
            glob(["lib/net45/*.dll"], allow_empty = True) or \\
            glob(["lib/net40*/*.dll"], allow_empty = True) or \\
            glob(["lib/net35/*.dll"], allow_empty = True) or \\
            glob(["lib/net20/*.dll"], allow_empty = True) or \\
            glob(["lib/netstandard2.0/*.dll"], allow_empty = True) or \\
            glob(["lib/netstandard1.*/*.dll"], allow_empty = True) or \\
            glob(["lib/*.dll"], allow_empty = True) or \\
            _all_dlls

net_import_library(
    name = "net",
    src = _net_dlls[0],
    version = "{version}",
    visibility = ["//visibility:public"],
)

[net_import_library(
    name = dll.split("/")[-1].replace(".dll", "").lower().replace(".", "_"),
    src = dll,
    version = "{version}",
    visibility = ["//visibility:public"],
) for dll in _net_dlls[1:]]
""".format(version = ctx.attr.version),
    )

simple_nuget_repo = repository_rule(
    implementation = _simple_nuget_repo_impl,
    attrs = {
        "package": attr.string(mandatory = True),
        "version": attr.string(mandatory = True),
        "sha256": attr.string(mandatory = True),
    },
)
