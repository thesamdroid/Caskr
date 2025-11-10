import { expect, test } from '@playwright/test'
import { stubDashboardData } from './support/apiStubs'

test.describe('dashboard', () => {
  test('expands an order card to reveal outstanding tasks', async ({ page }) => {
    await stubDashboardData(page, {
      orders: [
        { id: 1, name: 'Bourbon Order', statusId: 1, ownerId: 1, spiritTypeId: 1, quantity: 10, mashBillId: 1 },
        { id: 2, name: 'Bottled Batch', statusId: 2, ownerId: 1, spiritTypeId: 1, quantity: 4, mashBillId: 1 }
      ],
      statuses: [
        { id: 1, name: 'In Progress', statusTasks: [{ id: 10, name: 'Review paperwork' }] },
        { id: 2, name: 'Completed', statusTasks: [] }
      ],
      tasksByOrder: {
        1: [
          { id: 10, name: 'Review paperwork', orderId: 1, assigneeId: null, isComplete: false },
          { id: 11, name: 'Finalize labels', orderId: 1, assigneeId: 1, isComplete: true }
        ],
        2: [
          { id: 20, name: 'Archive documents', orderId: 2, assigneeId: null, isComplete: true }
        ]
      }
    })

    await page.goto('/')

    const orderCard = page.locator('.order-card', { hasText: 'Bourbon Order' })
    await expect(orderCard).toBeVisible()

    await orderCard.getByRole('button', { name: 'Expand' }).click()
    await expect(orderCard.getByRole('button', { name: 'Collapse' })).toBeVisible()

    await expect(orderCard.getByText('Review paperwork')).toBeVisible()
    await expect(orderCard.getByText('Finalize labels')).toBeVisible()
  })

  test('displays dashboard statistics derived from API data', async ({ page }) => {
    await stubDashboardData(page, {
      orders: [
        { id: 1, name: 'Bourbon Order', statusId: 1, ownerId: 1, spiritTypeId: 1, quantity: 10, mashBillId: 1 },
        { id: 2, name: 'Bottled Batch', statusId: 2, ownerId: 1, spiritTypeId: 1, quantity: 4, mashBillId: 1 }
      ],
      statuses: [
        { id: 1, name: 'In Progress', statusTasks: [{ id: 10, name: 'Review paperwork' }] },
        { id: 2, name: 'Completed', statusTasks: [] }
      ],
      tasksByOrder: {
        1: [
          { id: 10, name: 'Review paperwork', orderId: 1, assigneeId: null, isComplete: false },
          { id: 11, name: 'Finalize labels', orderId: 1, assigneeId: 1, isComplete: true }
        ],
        2: [
          { id: 20, name: 'Archive documents', orderId: 2, assigneeId: null, isComplete: true }
        ]
      }
    })

    await page.goto('/')

    const activeOrdersCard = page.locator('.stat-card', { hasText: 'Active Orders' })
    await expect(activeOrdersCard.locator('.stat-value')).toHaveText('2')

    const completedOrdersCard = page.locator('.stat-card', { hasText: 'Completed Orders' })
    await expect(completedOrdersCard.locator('.stat-value')).toHaveText('1')

    const totalTasksCard = page.locator('.stat-card', { hasText: 'Total Tasks' })
    await expect(totalTasksCard.locator('.stat-value')).toHaveText('3')
    await expect(totalTasksCard.locator('.stat-change')).toContainText('1 remaining')

    const progressCard = page.locator('.stat-card', { hasText: 'Overall Progress' })
    await expect(progressCard.locator('.stat-value')).toHaveText('67%')
  })
})
