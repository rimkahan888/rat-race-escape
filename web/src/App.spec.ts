import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import App from './App.vue'

describe('App', () => {
  it('opens on a new table with the clone as the default seat', () => {
    const wrapper = mount(App)

    expect(wrapper.text()).toContain('Rat Race Escape')
    expect(wrapper.text()).toContain('No game in progress')
    expect(wrapper.text()).toContain('Me vs my clone')
    expect(wrapper.get('[aria-label="Game table"]').text()).toContain('Sit down')
  })
})