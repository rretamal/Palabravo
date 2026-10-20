import { readFileSync, existsSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { resolve, dirname } from 'node:path'
const content = resolve(dirname(fileURLToPath(import.meta.url)), '../public/content')
const read = name => JSON.parse(readFileSync(resolve(content, name), 'utf8'))
const requireValue = (condition, message) => { if (!condition) throw new Error(message) }
function validatePuzzle(puzzle, explanations = true) {
  requireValue(puzzle.id && puzzle.title && puzzle.contentVersion >= 1, 'Incomplete puzzle')
  if ((puzzle.dynamic ?? 'connections') === 'connections') {
    requireValue(puzzle.groups.length === 4, `${puzzle.id}: groups`)
    const words = puzzle.groups.flatMap(group => {
      requireValue(group.words.length === 4 && group.category && group.hint && (!explanations || group.explanation), `${puzzle.id}: group fields`)
      return group.words
    })
    requireValue(new Set(words.map(w => w.toLocaleLowerCase('es'))).size === 16, `${puzzle.id}: repeated words`)
  } else {
    requireValue(puzzle.questions.length >= 2 && puzzle.questions.length <= 5, `${puzzle.id}: question count`)
    for (const q of puzzle.questions) {
      requireValue(q.prompt && q.answer && q.answer.length <= 80 && q.explanation && q.hint, `${puzzle.id}: question fields`)
      if (puzzle.dynamic !== 'recall') requireValue(q.options.length === 4 && new Set(q.options).size === 4 && q.options.includes(q.answer), `${puzzle.id}: options`)
      if (puzzle.dynamic === 'pieces') requireValue(q.fragments.length === 4 && new Set(q.fragments).size === 4 && q.solutionParts.length === 2 && q.solutionParts.every(p => q.fragments.includes(p)) && q.solutionParts.join('').toLocaleLowerCase('es') === q.answer.toLocaleLowerCase('es'), `${puzzle.id}: pieces`)
      if (puzzle.dynamic === 'visual') requireValue(q.imageSource && (/^https:\/\//.test(q.imageSource) || existsSync(resolve(content, q.imageSource))), `${puzzle.id}: missing image`)
    }
  }
}
const path = read('v2/puzzles.json')
const legacy = read('puzzles.json')
requireValue(path.length === legacy.length && path.every((p,i) => p.id === legacy[i].id && p.order === legacy[i].order), 'Path identities changed')
path.forEach(p => validatePuzzle(p))
const daily = read('daily.json')
requireValue(daily.timeZone === 'UTC' && daily.entries.length >= 14, 'Daily calendar coverage')
const dates = new Set()
for (const e of daily.entries) {
  requireValue(/^\d{4}-\d{2}-\d{2}$/.test(e.date) && e.puzzle.id === `daily-${e.date}` && !dates.has(e.date), 'Daily identity')
  dates.add(e.date); validatePuzzle(e.puzzle)
}
const weeklies = read('weekly.json').challenges.sort((a,b) => a.startsAt.localeCompare(b.startsAt))
const ids = new Set(), badges = new Set()
for (const [i,w] of weeklies.entries()) {
  requireValue(!ids.has(w.id) && !badges.has(w.badge.id), 'Repeated weekly identity')
  ids.add(w.id); badges.add(w.badge.id)
  requireValue(Date.parse(w.endsAt) > Date.parse(w.startsAt) && (!i || Date.parse(w.startsAt) >= Date.parse(weeklies[i-1].endsAt)), 'Weekly overlap')
  validatePuzzle(w.puzzle)
  requireValue(w.finalQuestions.length >= 2 && w.finalQuestions.every(q => q.options.length === 4 && new Set(q.options).size === 4 && q.options.includes(q.answer) && q.explanation), 'Weekly answers')
}
console.log(`Validated ${path.length} path puzzles, ${daily.entries.length} daily entries and ${weeklies.length} weekly events.`)
