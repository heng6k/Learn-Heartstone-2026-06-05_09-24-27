const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const test = require('node:test')

const root = path.resolve(__dirname, '..')
const schema = JSON.parse(fs.readFileSync(path.join(root, 'schema', 'scenario-share.schema.json'), 'utf8'))
const golden = JSON.parse(fs.readFileSync(path.join(root, 'fixtures', 'scenario-share-golden.json'), 'utf8'))
const localFixtures = require('../fixtures/scenarios')

function assertRequired(value, required, label) {
  for (const property of required) {
    assert.ok(Object.prototype.hasOwnProperty.call(value, property), label + ' missing ' + property)
  }
}

test('golden fixture satisfies the frozen v1 envelope and identity rules', () => {
  assertRequired(golden, schema.required, 'contract')
  assertRequired(golden.summary, schema.$defs.summary.required, 'summary')
  assertRequired(golden.compatibility, schema.$defs.compatibility.required, 'compatibility')
  assertRequired(golden.content, schema.$defs.content.required, 'content')
  assertRequired(golden.handoff, schema.$defs.handoff.required, 'handoff')

  assert.equal(golden.schemaVersion, 1)
  assert.match(golden.shareCode, new RegExp(schema.properties.shareCode.pattern))
  assert.match(golden.contentHash, new RegExp(schema.properties.contentHash.pattern))
  assert.equal(golden.content.state.schemaVersion, golden.compatibility.scenarioSchemaVersion)
  assert.equal(golden.content.state.mechanicStateSchemaVersion, golden.compatibility.mechanicStateSchemaVersion)
  assert.equal(golden.content.state.gameVersionId, golden.compatibility.gameVersionId)
  assert.equal(golden.content.state.rulesetId, golden.compatibility.rulesetId)
  assert.equal(golden.content.state.rulesetRevision, golden.compatibility.rulesetRevision)
  assert.equal(golden.content.state.contentSnapshotId, golden.compatibility.contentSnapshotId)
  assert.equal(golden.content.state.contentFingerprint, golden.compatibility.contentFingerprint)
  assert.ok(golden.summary.finalComposition.length > 0)
  assert.ok(golden.content.steps.length > 0)
})

test('runtime golden projection stays equal to the compiler-backed JSON fixture', () => {
  const runtime = localFixtures.byCode[golden.shareCode].contract
  const expectedContent = Object.assign({}, golden.content)
  delete expectedContent.state
  const expected = Object.assign({}, golden, { content: expectedContent })

  assert.deepEqual(runtime, expected)
})
