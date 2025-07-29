import cv2
import numpy as np
import pyautogui
import time
import json
import os
import sys
import ctypes
from ctypes import wintypes
from datetime import datetime

class ImageRecognition:
    def __init__(self):
        # 设置PyAutoGUI参数
        pyautogui.FAILSAFE = True
        pyautogui.PAUSE = 0.1
        
        # 脚本存储路径
        self.script_dir = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'scripts')
        if not os.path.exists(self.script_dir):
            os.makedirs(self.script_dir)

    def capture_screen(self, region=None):
        """使用BitBlt实现高效屏幕捕获
        Args:
            region: 可选区域 (x, y, width, height)
        Returns:
            numpy数组格式的图像 (BGR)
        """
        if region:
            x, y, width, height = region
        else:
            # 获取全屏尺寸
            x, y = 0, 0
            width = user32.GetSystemMetrics(0)
            height = user32.GetSystemMetrics(1)

        # 创建内存DC和位图
        hdesktop = user32.GetDesktopWindow()
        hdc = user32.GetDC(hdesktop)
        hdc_mem = gdi32.CreateCompatibleDC(hdc)
        hbitmap = gdi32.CreateCompatibleBitmap(hdc, width, height)
        gdi32.SelectObject(hdc_mem, hbitmap)

        # 使用BitBlt捕获屏幕
        gdi32.BitBlt(hdc_mem, 0, 0, width, height, hdc, x, y, 0x00CC0020)

        # 获取位图信息
        bmi = BITMAPINFO()
        bmi.bmiHeader.biSize = ctypes.sizeof(BITMAPINFOHEADER)
        bmi.bmiHeader.biWidth = width
        bmi.bmiHeader.biHeight = -height  # 负值表示从上到下
        bmi.bmiHeader.biPlanes = 1
        bmi.bmiHeader.biBitCount = 24
        bmi.bmiHeader.biCompression = 0  # BI_RGB

        # 将位图数据转换为numpy数组
        buffer_size = width * height * 3
        buffer = ctypes.create_string_buffer(buffer_size)
        gdi32.GetDIBits(hdc, hbitmap, 0, height, buffer, ctypes.byref(bmi), 0)

        # 释放资源
        user32.ReleaseDC(hdesktop, hdc)
        gdi32.DeleteDC(hdc_mem)
        gdi32.DeleteObject(hbitmap)

        # 将BGR格式转换为RGB
        image = np.frombuffer(buffer.raw, dtype=np.uint8).reshape((height, width, 3))
        return cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

    def find_template(self, screenshot, template_path, threshold=0.8):
        """使用OpenCV模板匹配查找图像
        Args:
            screenshot: 屏幕截图
            template_path: 模板图像路径
            threshold: 匹配阈值
        Returns:
            匹配位置坐标 (x, y) 或 None
        """
        if not os.path.exists(template_path):
            return None

        template = cv2.imread(template_path)
        if template is None:
            return None

        # 转换为灰度图以提高匹配速度
        gray_screenshot = cv2.cvtColor(screenshot, cv2.COLOR_RGB2GRAY)
        gray_template = cv2.cvtColor(template, cv2.COLOR_RGB2GRAY)

        # 获取模板尺寸
        w, h = gray_template.shape[::-1]

        # 使用TM_CCOEFF_NORMED方法进行模板匹配
        res = cv2.matchTemplate(gray_screenshot, gray_template, cv2.TM_CCOEFF_NORMED)
        loc = np.where(res >= threshold)

        # 返回第一个匹配位置的中心坐标
        for pt in zip(*loc[::-1]):
            return (pt[0] + w//2, pt[1] + h//2)
        return None

    def recognize_and_act(self, template_path, action='click', threshold=0.8):
        """识别图像并执行指定操作
        Args:
            template_path: 模板图像路径
            action: 要执行的操作 ('click', 'double_click', 'right_click')
            threshold: 匹配阈值
        Returns:
            操作是否成功
        """
        screenshot = self.capture_screen()
        position = self.find_template(screenshot, template_path, threshold)

        if position:
            x, y = position
            print(f"找到图像匹配: {template_path} @ ({x},{y})")

            if action == 'click':
                return self.click_position(x, y)
            elif action == 'double_click':
                pyautogui.doubleClick(x, y)
                return True
            elif action == 'right_click':
                return self.click_position(x, y, button='right')

        return False

    def click_position(self, x, y, button='left'):
        """点击指定位置
        Args:
            x, y: 坐标
            button: 鼠标按钮 ('left', 'right', 'middle')
        """
        pyautogui.moveTo(x, y, duration=0.2)
        pyautogui.click(button=button)
        return True

    def record_script(self, duration=None):
        """录制脚本
        Args:
            duration: 录制时长(秒)，None表示手动停止
        Returns:
            脚本数据列表
        """
        script_data = []
        start_time = time.time()
        last_action_time = start_time
        last_mouse_state = {'left': False, 'right': False, 'middle': False}

        print("开始录制脚本...按Ctrl+C停止")
        try:
            while True:
                if duration and time.time() - start_time > duration:
                    break

                # 检测鼠标点击状态
                current_mouse_state = {
                    'left': pyautogui.mouseDown(button='left'),
                    'right': pyautogui.mouseDown(button='right'),
                    'middle': pyautogui.mouseDown(button='middle')
                }

                # 检测鼠标点击事件（按下时记录）
                for button in ['left', 'right', 'middle']:
                    # 检查鼠标按钮是否从释放变为按下状态
                    if current_mouse_state[button] and not last_mouse_state[button]:
                        x, y = pyautogui.position()
                        action_time = time.time() - start_time
                        delay = action_time - last_action_time

                        script_data.append({
                            'action': 'click',
                            'button': button,
                            'x': x,
                            'y': y,
                            'delay': round(delay, 2),
                            'timestamp': datetime.now().strftime('%H:%M:%S.%f')
                        })

                        last_action_time = action_time
                        print(f"记录点击: {button} @ ({x},{y})")

                # 更新鼠标状态
                last_mouse_state = current_mouse_state.copy()
                time.sleep(0.05)
        except KeyboardInterrupt:
            print("录制停止")
        except Exception as e:
            print(f"录制错误: {str(e)}")
        return script_data

    def save_script(self, script_data, script_name=None):
        """保存脚本为JSON文件
        Args:
            script_data: 脚本数据
            script_name: 脚本名称，None则自动生成
        Returns:
            保存路径
        """
        if not script_data:
            return None

        if not script_name:
            script_name = f"script_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
        script_path = os.path.join(self.script_dir, f"{script_name}.json")

        with open(script_path, 'w', encoding='utf-8') as f:
            json.dump(script_data, f, ensure_ascii=False, indent=2)

        return script_path

    def load_script(self, script_name):
        """加载脚本
        Args:
            script_name: 脚本名称(不含扩展名)
        Returns:
            脚本数据
        """
        script_path = os.path.join(self.script_dir, f"{script_name}.json")
        if not os.path.exists(script_path):
            return None

        with open(script_path, 'r', encoding='utf-8') as f:
            return json.load(f)

    def run_script(self, script_data):
        """执行脚本
        Args:
            script_data: 脚本数据
        Returns:
            执行结果
        """
        if not script_data:
            return False

        print("开始执行脚本...")
        try:
            for action in script_data:
                if action['action'] == 'click':
                    # 等待延迟
                    if 'delay' in action and action['delay'] > 0:
                        time.sleep(action['delay'])
                    # 执行点击
                    self.click_position(action['x'], action['y'], action['button'])
                    print(f"执行点击: {action['button']} @ ({action['x']},{action['y']})")
            return True
        except Exception as e:
            print(f"脚本执行错误: {str(e)}")
            return False

# 移除重复的main函数

if __name__ == "__main__":
    # 处理命令行参数
    if len(sys.argv) < 2:
        print("用法: python image_recognition.py [command] [args]")
        print("命令列表:")
        print("  record - 录制脚本")
        print("  run <script_name> - 执行指定脚本")
        print("  save <script_name> - 保存当前录制的脚本")
        sys.exit(1)

    ir = ImageRecognition()
    command = sys.argv[1].lower()

    try:
        if command == 'record':
            script_data = ir.record_script()
            # 自动保存录制的脚本
            script_name = f"script_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
            save_path = ir.save_script(script_data, script_name)
            if save_path:
                print(f"脚本已保存至: {save_path}")
            else:
                print("未记录到操作，未保存脚本")

        elif command == 'run':
            if len(sys.argv) < 3:
                print("请指定脚本名称: python image_recognition.py run <script_name>")
                sys.exit(1)
            script_name = sys.argv[2]
            script_data = ir.load_script(script_name)
            if script_data:
                success = ir.run_script(script_data)
                print(f"脚本执行{'成功' if success else '失败'}")
            else:
                print(f"找不到脚本: {script_name}")

        else:
            print(f"未知命令: {command}")
            sys.exit(1)

    except Exception as e:
        print(f"执行错误: {str(e)}")
        sys.exit(1)
    # 测试代码
    ir = ImageRecognition()
    # 截图测试
    screenshot = ir.capture_screen()
    cv2.imwrite('screenshot_test.jpg', screenshot)
    # 模板匹配测试
    # match_pos = ir.find_template(screenshot, 'template.jpg')
    # if match_pos:
    #     print(f"找到匹配位置: {match_pos}")
    # 录制测试
    # script = ir.record_script()
    # ir.save_script(script, 'test_script')
    # 执行测试
    # script = ir.load_script('test_script')
    # ir.run_script(script)
    # ...
    # BitBlt屏幕捕获所需的Windows API定义
    user32 = ctypes.WinDLL('user32', use_last_error=True)
    gdi32 = ctypes.WinDLL('gdi32', use_last_error=True)
    
    class BITMAPINFOHEADER(ctypes.Structure):
        _fields_ = [
            ('biSize', wintypes.DWORD),
            ('biWidth', ctypes.c_long),
            ('biHeight', ctypes.c_long),
            ('biPlanes', wintypes.WORD),
            ('biBitCount', wintypes.WORD),
            ('biCompression', wintypes.DWORD),
            ('biSizeImage', wintypes.DWORD),
            ('biXPelsPerMeter', ctypes.c_long),
            ('biYPelsPerMeter', ctypes.c_long),
            ('biClrUsed', wintypes.DWORD),
            ('biClrImportant', wintypes.DWORD)
        ]
    
    class BITMAPINFO(ctypes.Structure):
        _fields_ = [
            ('bmiHeader', BITMAPINFOHEADER),
            ('bmiColors', wintypes.DWORD * 3)
        ]