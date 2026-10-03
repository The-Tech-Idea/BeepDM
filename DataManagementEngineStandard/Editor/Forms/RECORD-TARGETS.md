# Captured Validation, LOV And Editor Targets

Status: incremental C/D work, not a global operation scheduler or host conformance closeout.

## Captured Manager Operations

[RecordTargets](FormsManager.RecordTargets.cs) captures registration/block/UoW,
collection/current-record identity, mode/query revision, item identity, a per-field/
operation request revision and declared readable field values before work runs.
Manager validation also shares an annotation revision: a nested newer field pass
supersedes an outer record pass even without edits, not just same-field overlap.
Byte values are copied; opaque reference values are identity checks, not deep graphs.
Missing declared fields fail capture rather than silently validating an incomplete target.
Getters, helpers, triggers, setters and observers run outside ownership monitors;
identity/revision checks surround those calls, not only the await.

Default UoWs and wrappers expose optional
[IUnitofWorkRecordRevision](../../../DataManagementModelsStandard/Editor/IUnitofWorkRecordRevision.cs).
Its canonical observed cursor/item/collection revision distinguishes A-to-B-to-A
movement or edits even when the original references/values return. It is separate
from staged-read revision so a later audit notification of the original edit does
not invalidate its own awaited validation. Disposed supported sources reject;
wrappers retain capability identity after disposal, never silently downgrade to a
legacy fallback. Legacy sources have registration event/value/reference checks,
not proof of unobserved changes or ABA behavior.

[ValidateField/ValidateBlock](FormsManager.Validation.cs) join lifetime accounting
and reject retired/reentrant target changes before annotating item errors. Record
validation copies dictionary shells rather than handing the live map to a helper.
The manager-owned ItemChanged rule/typed-value LOV path requires the event record
to be the captured current record. It passes linked lifetime cancellation to the
LOV trigger, awaits helper acknowledgement and refuses stale set/clear error work.
New same-field validation supersedes older annotations; cursor, edit, query, mode,
item or registration changes invalidate them. Existing helper events are not made
generation-safe by this manager boundary.

## LOV Outcomes And Selection

Optional [IFormsLovOutcomes](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IFormsLovOutcomes.cs)
exposes ShowLOVWithOutcomeAsync; ShowLOVAsync projects its
[FormLovResult](../../../DataManagementModelsStandard/Editor/Forms/Models/FormLovResult.cs)
as LOVResult. The result carries canonical block, form/registration/request identity,
typed terminal state, load acknowledgement and separate selection-effect evidence.
Field request revisions belong to one registration/operation, not a universal UI token.

LOV definition identity, return field and copied related mappings are rechecked.
Selected values are captured before the trigger/load; external later mutation of
that selection cannot alter the pending assignments. Uncaptured/duplicate target
fields reject before provider work. Failure/exception/timeout/form-trigger-failure
does not run the default load; Success/Skipped permit it. Awaited nested LOV work
from its own triggers/providers rejects rather than recursing without a bound.

After load acknowledgement, every selection setter targets the captured record,
never a newly fetched CurrentItem. Expected own setter notifications can advance
the captured revision only by the expected canonical event count; nested or
independent-context edit/navigation, changed fields and retirement stop
later assignments. AppliedFields records each acknowledged setter. SelectionApplied
means the full mapping was acknowledged, not atomicity, database durability or exact
custom setter behavior. SelectionEffectsPossible remains true after an attempted
setter even if it failed. Partial writes are not rolled back: preserve that evidence
and reconcile, rather than blindly replaying the selection.

Pre-cancelled/post-close calls reject before admission. Admitted cancellation/close
waits for physical trigger/helper acknowledgement and returns a typed cancellation
without later manager selection. Public LOV work joins DisposeAsync/callback drain;
self-drain rejects. The helper load has no token overload: cancellation does not
forcibly stop it. HelperEffectsPossible denotes load invocation; helper cache/events
can occur before late rejection and are not undone. A failed/superseded result does
not expose its loaded records as manager success.

## Editor Popup Completion And UI Provider Boundary

[Editor operations](FormsManager.Editor.cs) expose optional
[IFormsEditorOutcomes](../../../DataManagementModelsStandard/Editor/Forms/Interfaces/IFormsEditorOutcomes.cs)
and [FormEditorResult](../../../DataManagementModelsStandard/Editor/Forms/Models/FormEditorResult.cs).
ShowEditorAsync projects accepted completion as legacy EditorResult; provider OK
alone is no longer a successful manager commit. Failed, denied, cancelled and
superseded completions project cancellation. Pre-cancelled/post-close calls throw
before admission. Admitted provider faults return typed failure instead of escaping
through the legacy method; use the optional API for diagnostics and effect evidence.

Editor requests capture the same record/registration/item/mode/query/observed
revision boundary, with same-field editor supersession. The named definition and
item EditorName are rechecked; the provider receives a separate definition copy.
Provider input mutations cannot change the registry. Direct mutable definition or
permission ABA is not versioned by this increment.

Before disclosure and again before write, editors enforce current block/item
insert versus update flags and block security. Existing tracking identifies added
rows even in CRUD mode. ENTER_QUERY/ReadOnly, disabled/hidden items and denied
field permissions reject. Masked fields reject raw-text popups even for admins;
no masked display placeholder is written back as actual data. Host-supplied
principals remain trusted inputs, not verified authentication or database policy.

ProviderInvoked/ProviderAcknowledged/ProviderCommitted describe dialog evidence;
WriteEffectsPossible/WriteAcknowledged describe the setter separately. Only a
current, permitted, acknowledged write produces Completed/Committed and Value.
Effects can remain after close/reentrant changes inside a setter: no rollback is
claimed and rejected results withhold the text. Preserve the evidence, do not replay.
The popup does not explicitly fire WHEN-VALIDATE-ITEM; ordinary item-change callbacks
still run through the existing validation path. Awaited nested editors reject.

The borrowed IEditorProvider owns dispatch, dismissal and physical cancellation
acknowledgement. Forms links caller/close cancellation and waits for completion
before releasing callback drain, including providers which ignore cancellation.
Do not self-drain from a provider. The manual dispatch-queue fixture tests this
boundary, not a concrete desktop dialog or IBlockView/IFieldPresenter conformance.
No parallel host interface or UI-library dependency is introduced.

## Remaining Gates

[UI binding](UI-BINDING-CONTRACTS.md) reuses this record boundary with immutable
public tokens, copied evidence for edits, security-revision checks and an opt-in
host/view/presenter helper. Binding capture rejects security helpers without the
snapshot revision capability. Default field policies now clone inputs/get results
and revision their explicit setter; use SetFieldSecurity to publish changes.
This does not add policy/definition revision pinning to every LOV/editor path.

This does not qualify LOV/validation provider policy or cache isolation: their targets
can differ from the block's entity/datasource. Raw helper events, mutable rule/definition
graphs, unobserved edits, opaque nested values and concurrent changes in the final
check-to-setter window remain open. General edit/commit/navigation scheduling, async
rule execution/context isolation, broader field permission/masking outside popups,
UI dispatcher/binding generations and real adapter conformance are separate work.

[RecordTargetTests](../Forms.Tests/RecordTargetTests.cs) exercises real default UoWs/
wrappers and default LOV loading/validation, including healthy and rejected selection,
cursor/edit ABA, repeated edits, replacement, close/drain, nested work, failed/null
loads, trigger failure and partial-write evidence. See [implementation evidence](IMPLEMENTATION-LOG.md).
