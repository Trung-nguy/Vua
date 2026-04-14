import { useState } from 'react'
import { Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { CheckCheck, ExternalLink } from 'lucide-react'
import { thongBaoAPI } from '@/api'
import { AppLayout } from '@/components/layout'
import { EmptyState, Spinner, Tabs } from '@/components/custom-ui'
import { fmtRelative } from '@/utils'

const NOTIF_ICONS: Record<string, string> = {
  DocumentSubmitted:   '📤',
  DocumentApproved:    '✅',
  DocumentRejected:    '❌',
  DocumentIssued:      '🔖',
  DocumentDistributed: '📬',
  OverdueWarning:      '⚠️',
  System:              '🔔',
}

export default function NotificationsPage() {
  const qc = useQueryClient()
  const [tab, setTab] = useState('all')

  const { data, isLoading } = useQuery({
    queryKey: ['thong-bao', tab],
    queryFn: () => thongBaoAPI.layDanhSach(tab === 'unread').then(r => r.data),
    refetchInterval: 20_000,
  })

  const markAllMut = useMutation({
    mutationFn: () => thongBaoAPI.danhDauTatCaDaDoc(),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['thong-bao'] }),
  })

  const markOneMut = useMutation({
    mutationFn: (id: number) => thongBaoAPI.danhDauDaDoc(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['thong-bao'] }),
  })

  const unreadCount = data?.filter(n => !n.daDoc).length ?? 0

  return (
    <AppLayout title="Thông báo">
      <div className="max-w-2xl mx-auto space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h1 className="page-title">Thông báo</h1>
            {unreadCount > 0 && <p className="page-subtitle">{unreadCount} chưa đọc</p>}
          </div>
          {unreadCount > 0 && (
            <button onClick={() => markAllMut.mutate()} className="btn-ghost btn-sm">
              <CheckCheck size={14} /> Đánh dấu tất cả đã đọc
            </button>
          )}
        </div>

        <Tabs
          tabs={[
            { key: 'all',    label: 'Tất cả' },
            { key: 'unread', label: 'Chưa đọc', count: unreadCount },
          ]}
          active={tab} onChange={setTab}
        />

        {isLoading ? (
          <div className="flex justify-center py-16"><Spinner size="lg" /></div>
        ) : data?.length === 0 ? (
          <EmptyState icon="🔔" title="Không có thông báo nào"
            desc={tab === 'unread' ? 'Bạn đã đọc tất cả thông báo' : 'Chưa có thông báo nào'} />
        ) : (
          <div className="card divide-y divide-ink-50 overflow-hidden">
            {data?.map((n, i) => (
              <motion.div key={n.id} initial={{ opacity: 0 }} animate={{ opacity: 1 }}
                transition={{ delay: i * 0.02 }}
                className={`flex items-start gap-4 px-5 py-4 cursor-pointer hover:bg-ink-50/60 transition-colors
                  ${!n.daDoc ? 'bg-jade-50/20' : ''}`}
                onClick={() => !n.daDoc && markOneMut.mutate(n.id)}>
                <div className={`w-9 h-9 rounded-full flex items-center justify-center flex-shrink-0 text-lg
                  ${!n.daDoc ? 'bg-jade-100' : 'bg-ink-100'}`}>
                  {NOTIF_ICONS[n.loaiThongBao] ?? '🔔'}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-start justify-between gap-2">
                    <p className={`text-sm leading-snug ${!n.daDoc ? 'font-semibold text-ink-900' : 'font-medium text-ink-700'}`}>
                      {n.tieuDe}
                    </p>
                    {!n.daDoc && (
                      <div className="w-2 h-2 rounded-full bg-jade-500 flex-shrink-0 mt-1.5" />
                    )}
                  </div>
                  <p className="text-xs text-ink-500 mt-0.5 line-clamp-2">{n.noiDung}</p>
                  <div className="flex items-center gap-3 mt-2">
                    <p className="text-xs text-ink-400">{fmtRelative(n.ngayTao)}</p>
                    {n.vanBanId && (
                      <Link to={`/van-ban/${n.vanBanId}`}
                        className="text-xs text-jade-600 hover:text-jade-700 flex items-center gap-1"
                        onClick={e => e.stopPropagation()}>
                        Xem văn bản <ExternalLink size={10} />
                      </Link>
                    )}
                  </div>
                </div>
              </motion.div>
            ))}
          </div>
        )}
      </div>
    </AppLayout>
  )
}
