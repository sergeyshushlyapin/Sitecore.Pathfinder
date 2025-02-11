// 1. Install NodeJS
// 2. Run 'npm update' to install/update node modules
//
// 3. Update version in this file
// 4. Update version in ./src/buildfiles/npm/package.json
// 5. Update version in assemblyinfo.cs files
//
// 6. Run 'gulp' to build Pathfinder without publishing
// 7. Or run 'gulp publish' to publish Pathfinder to npmjs.com, Nuget.org and the zip file directory
// 8. Or run 'gulp nightly' to publish Pathfinder to the nightly directory
//
// 9. Done

var version = "0.8.0-alpha";

var gulp = require("gulp");
var del = require("del");
var msbuild = require("gulp-msbuild");
var nugetpack = require("gulp-nuget-pack");
var runSequence = require("run-sequence");
var spawn = require("child_process").spawn;
var zip = require("gulp-zip");

// build project

gulp.task("build-project", function() {
    return gulp.src("./Sitecore.Pathfinder.sln").pipe(msbuild({
        targets: ["Clean", "Build"],
        configuration: "Debug",
        logCommand: false,
        verbosity: "minimal",
        maxcpucount: 0,
        toolsVersion: 14.0
    }));
});

// dist

gulp.task("clean-dist-directory", function() {
    return del("./build/dist");
});

gulp.task("build-dist-directory", ["clean-dist-directory"], function() {
    return gulp.src(["./bin/files/**/*", "./bin/licenses/**/*", "./bin/*.dll", "./bin/scc.exe", "./bin/scc.exe.config", "./bin/scconfig.json"], { base: "./bin/" }).
        pipe(gulp.dest("./build/dist"));
});

// npm

gulp.task("clean-npm-directory", function() {
    return del("./build/npm");
});

gulp.task("copy-npm-files", ["clean-npm-directory"], function() {
    return gulp.src(["./src/buildfiles/npm/package.json", "./src/buildfiles/npm/README.md"]).
        pipe(gulp.dest("./build/npm"));
});

gulp.task("copy-npm-directory", ["clean-npm-directory", "copy-npm-files"], function() {
    return gulp.src(["./build/dist/**/*"]).
        pipe(gulp.dest("./build/npm/"));
});

gulp.task("build-npm-package", ["copy-npm-directory"], function() {
    return spawn("npm.cmd", ["pack"], { stdio: "inherit", "cwd": "./build/npm/" });
});

gulp.task("publish-npm-package", ["copy-npm-directory"], function() {
    return spawn("npm.cmd", ["publish"], { stdio: "inherit", "cwd": "./build/npm/" });
});

// nuget

gulp.task("clean-nuget-package", function() {
    return del("./build/Sitecore.Pathfinder.nupkg");
});

gulp.task("build-nuget-package", ["clean-nuget-package"], function(callback) {
    return nugetpack({
            id: "Sitecore.Pathfinder",
            version: version,
            authors: "Jakob Christensen",
            owners: "Sitecore A/S",
            description: "Sitecore Pathfinder toolchain.",
            releaseNotes: "",
            summary: "Get started, get far, get happy.",
            language: "en-us",
            projectUrl: "https://github.com/JakobChristensen/Sitecore.Pathfinder",
            licenseUrl: "https://github.com/JakobChristensen/Sitecore.Pathfinder/blob/master/LICENSE",
            copyright: "Copyright 2016 by Sitecore A/S",
            requireLicenseAcceptance: false,
            dependencies: [
            ],
            tags: "Sitecore, Pathfinder, compilation, nuget, npm",
            excludes: ["./src/Sitecore.Pathfinder.Console/files/project/scc.cmd", "./src/Sitecore.Pathfinder.Console/files/project/sitecore.filetemplates"],
            outputDir: "./build"
        },
        [
            { src: "./build/dist", dest: "/content/sitecore.tools/" },
            { src: "./src/Sitecore.Pathfinder.Console/files/project", dest: "/content/" }
        ],
        callback
    );
});

gulp.task("publish-nuget-package", ["build-nuget-package"], function() {
    return spawn("../bin/nuget.exe", ["push", "Sitecore.Pathfinder." + version + ".nupkg"], { stdio: "inherit", "cwd": "./build/" });
});

// zip file

gulp.task("clean-zip-file", function() {
    return del("./build/Sitecore.Pathfinder.zip");
});

gulp.task("build-zip-file", ["clean-zip-file"], function() {
    return gulp.src(["build/dist/**/*"]).
        pipe(zip("Sitecore.Pathfinder.zip")).
        pipe(gulp.dest("build"));
});

// tasks

gulp.task("default", ["build"], function() {
});

gulp.task("build", function() {
    runSequence("build-project", "build-dist-directory", ["build-zip-file", "build-npm-package", "build-nuget-package"]);
});

gulp.task("publish", function() {
    runSequence("build-project", "build-dist-directory", ["publish-nuget-package", "publish-npm-package"]);
});

gulp.task("appveyor", function() {
    runSequence("build-dist-directory", ["build-zip-file", "build-npm-package", "build-nuget-package"]);
});
