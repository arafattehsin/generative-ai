import type { ReactNode } from 'react';
import { Box, Container, Group, Text, Badge, ActionIcon, Tooltip, rem } from '@mantine/core';
import { IconBrandGithub, IconSparkles } from '@tabler/icons-react';
import { useNavigate } from 'react-router-dom';

interface LayoutProps {
  children: ReactNode;
}

export function Layout({ children }: LayoutProps) {
  const navigate = useNavigate();

  return (
    <Box
      style={{
        minHeight: '100vh',
        background: 'linear-gradient(135deg, #0f172a 0%, #1e293b 50%, #0f172a 100%)',
        position: 'relative',
        overflow: 'hidden',
      }}
    >
      {/* Animated background elements */}
      <Box
        style={{
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          overflow: 'hidden',
          pointerEvents: 'none',
        }}
      >
        {/* Gradient orbs */}
        <Box
          style={{
            position: 'absolute',
            top: '-20%',
            left: '-10%',
            width: '40%',
            height: '40%',
            background: 'radial-gradient(circle, rgba(59, 130, 246, 0.15) 0%, transparent 70%)',
            filter: 'blur(60px)',
            animation: 'float 20s ease-in-out infinite',
          }}
        />
        <Box
          style={{
            position: 'absolute',
            bottom: '-20%',
            right: '-10%',
            width: '50%',
            height: '50%',
            background: 'radial-gradient(circle, rgba(139, 92, 246, 0.1) 0%, transparent 70%)',
            filter: 'blur(80px)',
            animation: 'float 25s ease-in-out infinite reverse',
          }}
        />
        <Box
          style={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            width: '60%',
            height: '60%',
            background: 'radial-gradient(circle, rgba(6, 182, 212, 0.08) 0%, transparent 70%)',
            filter: 'blur(100px)',
          }}
        />
      </Box>

      {/* Header */}
      <Box
        component="header"
        style={{
          position: 'sticky',
          top: 0,
          zIndex: 100,
          background: 'rgba(15, 23, 42, 0.8)',
          backdropFilter: 'blur(20px)',
          borderBottom: '1px solid rgba(255, 255, 255, 0.05)',
        }}
      >
        <Container size="xl" py="md">
          <Group justify="space-between" align="center">
            {/* Logo & Brand */}
            <Group
              gap="sm"
              style={{ cursor: 'pointer' }}
              onClick={() => navigate('/')}
            >
              <Box
                style={{
                  width: 42,
                  height: 42,
                  borderRadius: 12,
                  background: 'linear-gradient(135deg, #3b82f6 0%, #8b5cf6 100%)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  boxShadow: '0 0 20px rgba(59, 130, 246, 0.4)',
                }}
              >
                <IconSparkles size={24} color="white" stroke={1.5} />
              </Box>
              <Box>
                <Text
                  size="lg"
                  fw={700}
                  style={{
                    color: 'white',
                    letterSpacing: '-0.5px',
                    lineHeight: 1.2,
                  }}
                >
                  PolicyPack Builder
                </Text>
                <Text
                  size="xs"
                  c="dimmed"
                  style={{ opacity: 0.7 }}
                >
                  Powered by Microsoft Agent Framework
                </Text>
              </Box>
            </Group>

            {/* Right side navigation */}
            <Group gap="md">
              <Badge
                variant="light"
                color="teal"
                size="lg"
                radius="md"
                style={{
                  textTransform: 'none',
                  fontWeight: 500,
                }}
              >
                <Group gap={6}>
                  <Box
                    style={{
                      width: 8,
                      height: 8,
                      borderRadius: '50%',
                      background: '#10b981',
                      animation: 'pulse 2s ease-in-out infinite',
                    }}
                  />
                  System Online
                </Group>
              </Badge>

              <Tooltip label="View on GitHub" position="bottom">
                <ActionIcon
                  variant="subtle"
                  color="gray"
                  size="lg"
                  radius="md"
                  component="a"
                  href="https://github.com/arafattehsin/generative-ai"
                  target="_blank"
                  style={{
                    color: 'rgba(255, 255, 255, 0.7)',
                  }}
                >
                  <IconBrandGithub size={20} />
                </ActionIcon>
              </Tooltip>
            </Group>
          </Group>
        </Container>
      </Box>

      {/* Main content */}
      <Box
        component="main"
        style={{
          position: 'relative',
          zIndex: 1,
          paddingTop: rem(40),
          paddingBottom: rem(80),
        }}
      >
        {children}
      </Box>

      {/* Footer */}
      <Box
        component="footer"
        style={{
          position: 'relative',
          zIndex: 1,
          borderTop: '1px solid rgba(255, 255, 255, 0.05)',
          background: 'rgba(15, 23, 42, 0.5)',
          backdropFilter: 'blur(10px)',
        }}
      >
        <Container size="xl" py="lg">
          <Group justify="space-between">
            <Text size="sm" c="dimmed">
              © 2026 PolicyPack Builder · Built with Microsoft Agent Framework
            </Text>
            <Text size="sm" c="dimmed">
              Sequential Workflow Orchestration Demo
            </Text>
          </Group>
        </Container>
      </Box>
    </Box>
  );
}
