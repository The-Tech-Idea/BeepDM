# Phase 5: Release And Developer Experience

Status: proposed. Finding: F08. Start CI/discovery alongside Phase 1; final release
gates depend on all completed correctness work.

## Work Items

- [ ] P5-01 Add Studio and new regression projects to normal test discovery.
  Remove tracked bin/obj from the index in a dedicated change, retaining local
  files and appropriate ignore rules. Do not mix this cleanup with runtime fixes.
  Studio/reliability solution inclusion is already done; generated-output cleanup
  remains, including tracked .verify artifacts discovered in the follow-up review.
- [ ] P5-02 Add CI for all supported TFMs and operating systems, explicit test
  inventories, provider integration jobs, and diagnostic artifact collection.
- [ ] P5-03 Pin SDK and floating test dependencies; make external copy targets
  opt-in. Keep package icons/docs outputs local and unique to project/target.
- [ ] P5-04 Verify clean-checkout build/pack without sibling repositories or local
  package folders. Inspect package assets and validate standalone consumers.
- [ ] P5-05 Add public API compatibility checks, package-based smoke consumers,
  symbol/SourceLink verification, and a documented support/versioning policy.
- [ ] P5-06 Provide compiling desktop/CLI and scoped web examples for bootstrap,
  CRUD, migration, import, and sync. Exercise packaged APIs, not just project refs.
- [ ] P5-07 Reconcile developer docs and skills with verified contracts. Update
  affected repository .harness skills and directly installed Codex copies when implementation
  changes; keep proposed behavior clearly separate from available APIs.
  Historical .cursor/skills paths are absent in this checkout; verify active skill
  roots before editing, including existing duplicate installations.
- [ ] P5-08 Establish warning/nullable baselines and reduce them incrementally.
  Avoid broad warning suppression or unbounded refactoring during reliability fixes.

## Verification And Acceptance

- Normal test entry points run every intended project and pass with no unexplained
  failure. Green local tests are not substituted for all-TFM build/runtime coverage.
- A clean supported environment can build, test, and pack without external icon
  directories, sibling copy destinations, or pre-existing local artifacts.
- Test runs leave tracked source unchanged. Package consumers resolve and exercise
  the intended assets on each supported TFM.
- API compatibility failures require an explicit versioning decision; examples
  and skills describe tested behavior and current method signatures.
- Correct stale .github agent test inventories/failure claims alongside skills.
  CI records project/TFM counts so a green run cannot silently omit a suite.
- Exercise legacy eager startup and normal AddBeepRuntime host lifetimes in
  package consumers, including the documented separate-runtime ownership and
  transient alias rules; signature-only API checks cannot prove that behavior.

Back to [master review and tracker](MASTER-FRAMEWORK-TRACKER.md).
