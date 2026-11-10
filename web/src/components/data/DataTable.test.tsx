import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { DataTable, type DataTableColumn } from './DataTable'

interface Row {
  id: number
  name: string
}

const columns: Array<DataTableColumn<Row>> = [
  { key: 'id', header: 'ID', render: (row) => row.id },
  { key: 'name', header: '名称', render: (row) => row.name },
]

describe('DataTable', () => {
  it('renders headers and rows', () => {
    render(
      <DataTable
        columns={columns}
        rows={[{ id: 1, name: '渠道 A' }]}
        rowKey={(row) => row.id}
        ariaLabel="测试表格"
      />,
    )

    expect(screen.getByRole('table', { name: '测试表格' })).toBeInTheDocument()
    expect(screen.getByRole('columnheader', { name: 'ID' })).toBeInTheDocument()
    expect(screen.getByText('渠道 A')).toBeInTheDocument()
  })

  it('renders loading state before data is ready', () => {
    render(<DataTable columns={columns} rows={[]} rowKey={(row) => row.id} loading />)
    expect(screen.getByText('加载中…')).toBeInTheDocument()
  })

  it('renders empty state when no rows', () => {
    render(<DataTable columns={columns} rows={[]} rowKey={(row) => row.id} emptyTitle="空表" />)
    expect(screen.getByText('空表')).toBeInTheDocument()
  })

  it('handles row click', async () => {
    const onRowClick = vi.fn()
    render(
      <DataTable
        columns={columns}
        rows={[{ id: 1, name: '渠道 A' }]}
        rowKey={(row) => row.id}
        onRowClick={onRowClick}
      />,
    )

    await userEvent.click(screen.getByText('渠道 A'))
    expect(onRowClick).toHaveBeenCalledWith({ id: 1, name: '渠道 A' })
  })
})
