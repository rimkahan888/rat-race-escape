import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import App from './App.vue'

describe('App', () => {
  it('shows the game name and an empty table', () => {
    const wrapper = mount(App)

    expect(wrapper.text()).toContain('Rat Race Escape')
    expect(wrapper.text()).toContain('No game in progress')
    expect(wrapper.get('[aria-label="Game table"]').text()).toContain(
      'The month still owns the calendar.',
    )
  })
})
