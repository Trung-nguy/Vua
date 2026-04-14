import { Navigate, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from "@tanstack/react-query"
import { BrowserRouter } from "react-router-dom"
import { Toaster } from 'react-hot-toast'
import { TooltipProvider } from "@/components/ui/tooltip"
import { useAuthStore } from '@/stores/authStore'

import LoginPage            from '@/pages/auth/LoginPage'
import DashboardPage        from '@/pages/DashboardPage'
import DocumentListPage     from '@/pages/documents/DocumentListPage'
import DocumentDetailPage   from '@/pages/documents/DocumentDetailPage'
import CreateDocumentPage   from '@/pages/documents/CreateDocumentPage'
import EditDocumentPage     from '@/pages/documents/EditDocumentPage'
import { WorkflowBMPage, WorkflowKhoaPage } from '@/pages/workflow/WorkflowBMPage'
import IssuancePage         from '@/pages/workflow/IssuancePage'
import DistributionPage     from '@/pages/workflow/DistributionPage'
import InboxPage            from '@/pages/workflow/InboxPage'
import StatisticsPage       from '@/pages/StatisticsPage'
import NotificationsPage    from '@/pages/NotificationsPage'
import SettingsPage         from '@/pages/SettingsPage'
import UsersPage            from '@/pages/admin/UsersPage'
import DepartmentsPage      from '@/pages/admin/DepartmentsPage'

function PrivateRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuthStore()
  return isAuthenticated ? <>{children}</> : <Navigate to="/dang-nhap" replace />
}

function PublicRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuthStore()
  return isAuthenticated ? <Navigate to="/dashboard" replace /> : <>{children}</>
}

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
      refetchOnWindowFocus: false,
    },
  },
})

const App = () => (
  <QueryClientProvider client={queryClient}>
    <TooltipProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/dang-nhap" element={<PublicRoute><LoginPage /></PublicRoute>} />
          <Route path="/*" element={
            <PrivateRoute>
              <Routes>
                <Route path="dashboard"                 element={<DashboardPage />} />
                <Route path="van-ban"                   element={<DocumentListPage />} />
                <Route path="van-ban/tao-moi"           element={<CreateDocumentPage />} />
                <Route path="van-ban/:id"               element={<DocumentDetailPage />} />
                <Route path="van-ban/:id/chinh-sua"     element={<EditDocumentPage />} />
                <Route path="workflow/xac-minh"         element={<WorkflowBMPage />} />
                <Route path="workflow/phe-duyet"        element={<WorkflowKhoaPage />} />
                <Route path="so-hieu"                   element={<IssuancePage />} />
                <Route path="phan-phoi"                 element={<DistributionPage />} />
                <Route path="phan-phoi/hop-thu-den"     element={<InboxPage />} />
                <Route path="thong-ke"                  element={<StatisticsPage />} />
                <Route path="thong-bao"                 element={<NotificationsPage />} />
                <Route path="cai-dat"                   element={<SettingsPage />} />
                <Route path="admin/nguoi-dung"          element={<UsersPage />} />
                <Route path="admin/bo-mon"              element={<DepartmentsPage />} />
                <Route path="*"                         element={<Navigate to="/dashboard" replace />} />
                <Route index                            element={<Navigate to="/dashboard" replace />} />
              </Routes>
            </PrivateRoute>
          } />
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
        </Routes>
        <Toaster
          position="top-right"
          toastOptions={{
            duration: 3500,
            style: {
              borderRadius: '12px',
              fontSize: '13px',
              fontFamily: 'Be Vietnam Pro, sans-serif',
              boxShadow: '0 4px 20px rgba(0,0,0,0.12)',
              maxWidth: '360px',
            },
            success: { iconTheme: { primary: '#1ea06a', secondary: '#fff' } },
            error:   { iconTheme: { primary: '#f43f5e', secondary: '#fff' } },
          }}
        />
      </BrowserRouter>
    </TooltipProvider>
  </QueryClientProvider>
)

export default App
