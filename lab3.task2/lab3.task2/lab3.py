import sys
import logging
import os

sys.stdout.reconfigure(encoding='utf-8')

folder = os.path.dirname(os.path.abspath(__file__))
logging.basicConfig(filename=f'{folder}/pipe_log.txt', level=logging.INFO, encoding='utf-8')

def main():
    
    for line in sys.stdin:
        number_str = line.strip()
        if number_str == "exit":
            break
            
        # Формуємо відповідь і відправляємо її назад у C#
        result = f"Обчислено в Python: {number_str}"
        logging.info(f"Отримано через I/O anonymous pipe: {number_str}")

        sys.stdout.write(result + '\n')
        sys.stdout.flush()

if __name__ == "__main__":
    main()